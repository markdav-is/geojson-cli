using System.Text;
using System.Text.Json;

namespace GeoJsonCli;

/// <summary>
/// Generates a SQL CREATE TABLE DDL statement from a GeoJSON document.
/// </summary>
public static class SchemaGenerator
{
    /// <summary>
    /// Produces a CREATE TABLE DDL string for the given GeoJSON and table name.
    /// </summary>
    public static string GenerateDdl(JsonElement geojson, string tableName)
    {
        var features = GetFeatures(geojson);
        var columns = TypeInference.InferColumns(features);

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {QuoteName(tableName)} (");

        var defs = columns.Select(c => $"  {QuoteName(c.Name)} {c.Type}").ToList();
        defs.Add("  geometry TEXT");

        sb.Append(string.Join(",\n", defs));
        sb.AppendLine();
        sb.AppendLine(");");

        return sb.ToString();
    }

    /// <summary>
    /// Extracts the features array from a FeatureCollection, or wraps a single Feature.
    /// </summary>
    public static IReadOnlyList<JsonElement> GetFeatures(JsonElement geojson)
    {
        if (!geojson.TryGetProperty("type", out var typeProp))
            return [];

        var type = typeProp.GetString();

        if (type == "FeatureCollection" &&
            geojson.TryGetProperty("features", out var features) &&
            features.ValueKind == JsonValueKind.Array)
        {
            return [.. features.EnumerateArray()];
        }

        if (type == "Feature")
            return [geojson];

        return [];
    }

    /// <summary>
    /// Wraps an identifier in double quotes, escaping any internal double quotes.
    /// </summary>
    public static string QuoteName(string name) =>
        $"\"{name.Replace("\"", "\"\"")}\"";
}
