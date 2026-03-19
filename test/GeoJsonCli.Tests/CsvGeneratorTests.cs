using System.Text.Json;
using GeoJsonCli;

namespace GeoJsonCli.Tests;

public class CsvGeneratorTests
{
    private static readonly JsonElement SampleGeoJson = LoadSample();

    [Fact]
    public void GenerateCsv_HeaderContainsAllKeysAndGeometry()
    {
        var csv = CsvGenerator.GenerateCsv(SampleGeoJson);
        var firstLine = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[0];
        Assert.Equal("name,height,floors,area,landmark,geometry", firstLine);
    }

    [Fact]
    public void GenerateCsv_CorrectNumberOfDataRows()
    {
        var csv = CsvGenerator.GenerateCsv(SampleGeoJson);
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 3 features
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void GenerateCsv_IncludesWktGeometry()
    {
        var csv = CsvGenerator.GenerateCsv(SampleGeoJson);
        Assert.Contains("POINT(-73.9857 40.7484)", csv);
        Assert.Contains("LINESTRING(-74.006 40.7128, -73.9857 40.7484)", csv);
        Assert.Contains("POLYGON(", csv);
    }

    [Fact]
    public void GenerateCsv_NullPropertyRenderedAsEmptyField()
    {
        var csv = CsvGenerator.GenerateCsv(SampleGeoJson);
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        // Second data row (index 2) is "Lower Manhattan Triangle" which has height=null
        var fields = lines[2].Split(',');
        Assert.Equal(string.Empty, fields[1]); // height column
    }

    [Fact]
    public void EscapeField_PlainValue_Unchanged()
    {
        Assert.Equal("hello", CsvGenerator.EscapeField("hello"));
    }

    [Fact]
    public void EscapeField_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CsvGenerator.EscapeField(string.Empty));
    }

    [Fact]
    public void EscapeField_ContainsComma_Quoted()
    {
        Assert.Equal("\"a,b\"", CsvGenerator.EscapeField("a,b"));
    }

    [Fact]
    public void EscapeField_ContainsDoubleQuote_EscapedAndQuoted()
    {
        Assert.Equal("\"say \"\"hi\"\"\"", CsvGenerator.EscapeField("say \"hi\""));
    }

    [Fact]
    public void EscapeField_ContainsNewline_Quoted()
    {
        Assert.Equal("\"line1\nline2\"", CsvGenerator.EscapeField("line1\nline2"));
    }

    private static JsonElement LoadSample()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.geojson");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }
}
