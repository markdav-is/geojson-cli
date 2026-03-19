using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoJsonCli;

public class GeoJsonFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; } = new();
}

public class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public GeoJsonGeometry? Geometry { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, JsonElement>? Properties { get; set; }
}

public class GeoJsonGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public JsonElement Coordinates { get; set; }
}

public enum ColumnType
{
    Integer,
    Real,
    Boolean,
    Text
}

public class ColumnSchema
{
    public string Name { get; set; } = string.Empty;
    public ColumnType Type { get; set; }
    public bool Nullable { get; set; }

    public string SqlType() => Type switch
    {
        ColumnType.Integer => "INTEGER",
        ColumnType.Real => "REAL",
        ColumnType.Boolean => "BOOLEAN",
        ColumnType.Text => "TEXT",
        _ => "TEXT"
    };
}

public class TableSchema
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnSchema> Columns { get; set; } = new();
}
