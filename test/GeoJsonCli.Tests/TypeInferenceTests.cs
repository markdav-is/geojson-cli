using System.Text.Json;
using GeoJsonCli;

namespace GeoJsonCli.Tests;

public class TypeInferenceTests
{
    [Fact]
    public void InferSqlType_EmptyValues_ReturnsText()
    {
        Assert.Equal("TEXT", TypeInference.InferSqlType([]));
    }

    [Fact]
    public void InferSqlType_BooleanValues_ReturnsBoolean()
    {
        var values = ParseValues("[true, false, true]");
        Assert.Equal("BOOLEAN", TypeInference.InferSqlType(values));
    }

    [Fact]
    public void InferSqlType_IntegerValues_ReturnsInteger()
    {
        var values = ParseValues("[1, 2, 3]");
        Assert.Equal("INTEGER", TypeInference.InferSqlType(values));
    }

    [Fact]
    public void InferSqlType_FloatValues_ReturnsDoublePrecision()
    {
        var values = ParseValues("[1.5, 2.3]");
        Assert.Equal("DOUBLE PRECISION", TypeInference.InferSqlType(values));
    }

    [Fact]
    public void InferSqlType_StringValues_ReturnsText()
    {
        var values = ParseValues("""["hello", "world"]""");
        Assert.Equal("TEXT", TypeInference.InferSqlType(values));
    }

    [Fact]
    public void InferSqlType_MixedTypes_ReturnsText()
    {
        var values = ParseValues("""[1, "hello"]""");
        Assert.Equal("TEXT", TypeInference.InferSqlType(values));
    }

    [Fact]
    public void InferColumns_InfersTypesFromFeatures()
    {
        const string json = """
            [
              {"type":"Feature","geometry":null,"properties":{"name":"A","count":1,"active":true}},
              {"type":"Feature","geometry":null,"properties":{"name":"B","count":2,"active":false}}
            ]
            """;

        var features = ParseArray(json);
        var cols = TypeInference.InferColumns(features).ToDictionary(c => c.Name, c => c.Type);

        Assert.Equal("TEXT", cols["name"]);
        Assert.Equal("INTEGER", cols["count"]);
        Assert.Equal("BOOLEAN", cols["active"]);
    }

    [Fact]
    public void InferColumns_NullValuesIgnoredForTypeInference()
    {
        const string json = """
            [
              {"type":"Feature","geometry":null,"properties":{"name":null}},
              {"type":"Feature","geometry":null,"properties":{"name":"hello"}}
            ]
            """;

        var features = ParseArray(json);
        var cols = TypeInference.InferColumns(features).ToDictionary(c => c.Name, c => c.Type);
        Assert.Equal("TEXT", cols["name"]);
    }

    [Fact]
    public void InferColumns_MissingPropertiesObject_Handled()
    {
        const string json = """
            [
              {"type":"Feature","geometry":null,"properties":null},
              {"type":"Feature","geometry":null,"properties":{"x":1}}
            ]
            """;

        var features = ParseArray(json);
        var cols = TypeInference.InferColumns(features).ToDictionary(c => c.Name, c => c.Type);
        Assert.Equal("INTEGER", cols["x"]);
    }

    private static IReadOnlyList<JsonElement> ParseValues(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static IReadOnlyList<JsonElement> ParseArray(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }
}
