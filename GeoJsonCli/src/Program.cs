using System.CommandLine;
using System.Text.Json;
using GeoJsonCli;

var inputArg = new Argument<FileInfo>(
    name: "input",
    description: "Path to the GeoJSON file to process");

var outputOption = new Option<DirectoryInfo?>(
    aliases: ["--output", "-o"],
    description: "Output directory for generated files (defaults to input file directory)");

var tableOption = new Option<string?>(
    aliases: ["--table", "-t"],
    description: "Table name for the DDL (defaults to the input filename without extension)");

var noHeaderOption = new Option<bool>(
    aliases: ["--no-header"],
    description: "Omit the header row from CSV output");

var rootCommand = new RootCommand("Extract schema (DDL) and data (CSV) from a GeoJSON file")
{
    inputArg,
    outputOption,
    tableOption,
    noHeaderOption
};

rootCommand.SetHandler((FileInfo input, DirectoryInfo? output, string? table, bool noHeader) =>
{
    try
    {
        if (!input.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {input.FullName}");
            Environment.Exit(1);
        }

        var outputDir = output?.FullName ?? input.DirectoryName ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);

        var tableName = table ?? Path.GetFileNameWithoutExtension(input.Name);

        Console.WriteLine($"Reading {input.FullName}...");

        var json = File.ReadAllText(input.FullName);
        GeoJsonFeatureCollection? featureCollection;

        try
        {
            featureCollection = JsonSerializer.Deserialize<GeoJsonFeatureCollection>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Error: Failed to parse GeoJSON: {ex.Message}");
            Environment.Exit(1);
            return;
        }

        if (featureCollection == null)
        {
            Console.Error.WriteLine("Error: Could not deserialize GeoJSON.");
            Environment.Exit(1);
            return;
        }

        if (!string.Equals(featureCollection.Type, "FeatureCollection", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Error: Expected a GeoJSON FeatureCollection but got type '{featureCollection.Type}'.");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine($"Found {featureCollection.Features.Count} feature(s). Inferring schema...");

        var schema = SchemaInferrer.Infer(tableName, featureCollection);

        // Write DDL
        var ddlPath = Path.Combine(outputDir, $"{tableName}.sql");
        var ddl = DdlGenerator.Generate(schema);
        File.WriteAllText(ddlPath, ddl);
        Console.WriteLine($"DDL written to:  {ddlPath}");

        // Write CSV
        var csvPath = Path.Combine(outputDir, $"{tableName}.csv");
        CsvExporter.Export(schema, featureCollection, csvPath);
        Console.WriteLine($"CSV written to:  {csvPath}");

        Console.WriteLine($"\nSchema columns ({schema.Columns.Count}):");
        foreach (var col in schema.Columns)
        {
            var nullStr = col.Nullable ? "NULL" : "NOT NULL";
            Console.WriteLine($"  {col.Name} {col.SqlType()} {nullStr}");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Unexpected error: {ex.Message}");
        Environment.Exit(1);
    }
}, inputArg, outputOption, tableOption, noHeaderOption);

return await rootCommand.InvokeAsync(args);
