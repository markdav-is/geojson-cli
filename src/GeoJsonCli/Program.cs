using System.Text.Json;
using GeoJsonCli;

// ---------------------------------------------------------------------------
// Argument parsing
// ---------------------------------------------------------------------------

string? inputPath = null;
string outputDir = ".";
string? tableName = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-o" or "--output" when i + 1 < args.Length:
            outputDir = args[++i];
            break;

        case "-t" or "--table" when i + 1 < args.Length:
            tableName = args[++i];
            break;

        case "-h" or "--help":
            PrintHelp();
            return 0;

        case "-V" or "--version":
            Console.WriteLine("1.0.0");
            return 0;

        default:
            if (!args[i].StartsWith('-'))
                inputPath = args[i];
            else
            {
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                PrintHelp();
                return 1;
            }
            break;
    }
}

if (inputPath is null)
{
    Console.Error.WriteLine("Error: an input GeoJSON file is required.");
    PrintHelp();
    return 1;
}

var resolvedInput = Path.GetFullPath(inputPath);

if (!File.Exists(resolvedInput))
{
    Console.Error.WriteLine($"Error: File not found: {resolvedInput}");
    return 1;
}

// ---------------------------------------------------------------------------
// Parse GeoJSON
// ---------------------------------------------------------------------------

JsonElement geojson;
try
{
    using var stream = File.OpenRead(resolvedInput);
    using var doc = await JsonDocument.ParseAsync(stream);
    geojson = doc.RootElement.Clone();
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Error: Failed to parse GeoJSON: {ex.Message}");
    return 1;
}

if (!geojson.TryGetProperty("type", out _))
{
    Console.Error.WriteLine("Error: Invalid GeoJSON – missing \"type\" property.");
    return 1;
}

// ---------------------------------------------------------------------------
// Determine names and paths
// ---------------------------------------------------------------------------

var baseName = Path.GetFileNameWithoutExtension(resolvedInput);
var table = tableName ?? baseName;
var resolvedOutput = Path.GetFullPath(outputDir);
Directory.CreateDirectory(resolvedOutput);

// ---------------------------------------------------------------------------
// Write DDL + CSV (one pair per detected schema group)
// ---------------------------------------------------------------------------

var groups = FeatureSplitter.SplitBySchema(SchemaGenerator.GetFeatures(geojson), table);

foreach (var (groupName, groupFeatures) in groups)
{
    var ddl = SchemaGenerator.GenerateDdl(groupFeatures, groupName);
    var ddlPath = Path.Combine(resolvedOutput, $"{groupName}.sql");
    await File.WriteAllTextAsync(ddlPath, ddl);
    Console.WriteLine($"DDL written to: {ddlPath}");

    var csv = CsvGenerator.GenerateCsv(groupFeatures);
    var csvPath = Path.Combine(resolvedOutput, $"{groupName}.csv");
    await File.WriteAllTextAsync(csvPath, csv);
    Console.WriteLine($"CSV written to: {csvPath}");
}

return 0;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static void PrintHelp()
{
    Console.WriteLine("""
        geojson-cli <input> [options]

        Extract the schema (DDL) and data (CSV) from a GeoJSON file.

        Arguments:
          <input>              Path to the input GeoJSON file

        Options:
          -o, --output <dir>   Output directory for generated files (default: .)
          -t, --table  <name>  Override the SQL table name (default: input filename)
          -V, --version        Print version
          -h, --help           Show this help message
        """);
}
