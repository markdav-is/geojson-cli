using System.Text.Json;
using GeoJsonCli;

namespace GeoJsonCli.Tests;

public class SchemaGeneratorTests
{
    private static readonly JsonElement SampleGeoJson = LoadSample();

    [Fact]
    public void GenerateDdl_IncludesTableName()
    {
        var ddl = SchemaGenerator.GenerateDdl(SampleGeoJson, "my_table");
        Assert.Contains("CREATE TABLE \"my_table\"", ddl);
    }

    [Fact]
    public void GenerateDdl_IncludesPropertyColumns()
    {
        var ddl = SchemaGenerator.GenerateDdl(SampleGeoJson, "sample");
        Assert.Contains("\"name\" TEXT", ddl);
        Assert.Contains("\"floors\" INTEGER", ddl);
        Assert.Contains("\"landmark\" BOOLEAN", ddl);
        Assert.Contains("\"area\" DOUBLE PRECISION", ddl);
    }

    [Fact]
    public void GenerateDdl_IncludesGeometryColumn()
    {
        var ddl = SchemaGenerator.GenerateDdl(SampleGeoJson, "sample");
        Assert.Contains("geometry TEXT", ddl);
    }

    [Fact]
    public void GenerateDdl_EndsWithSemicolon()
    {
        var ddl = SchemaGenerator.GenerateDdl(SampleGeoJson, "sample");
        Assert.EndsWith(");" + Environment.NewLine, ddl);
    }

    [Fact]
    public void GenerateDdl_SingleFeatureInput()
    {
        const string json = """
            {
              "type": "Feature",
              "geometry": {"type":"Point","coordinates":[0,0]},
              "properties": {"id": 1, "label": "x"}
            }
            """;
        using var doc = JsonDocument.Parse(json);
        var feature = doc.RootElement.Clone();

        var ddl = SchemaGenerator.GenerateDdl(feature, "pts");
        Assert.Contains("\"id\" INTEGER", ddl);
        Assert.Contains("\"label\" TEXT", ddl);
    }

    [Fact]
    public void QuoteName_EscapesInternalDoubleQuotes()
    {
        Assert.Equal("\"col\"\"name\"", SchemaGenerator.QuoteName("col\"name"));
    }

    private static JsonElement LoadSample()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.geojson");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }
}
