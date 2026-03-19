using System.Text;
using System.Text.Json;

namespace GeoJsonCli;

/// <summary>
/// Generates a CSV file from a GeoJSON document.
/// Each row represents one Feature; the final column is the geometry in WKT format.
/// </summary>
public static class CsvGenerator
{
    /// <summary>
    /// Produces a CSV string for the given GeoJSON document.
    /// </summary>
    public static string GenerateCsv(JsonElement geojson) =>
        GenerateCsv(SchemaGenerator.GetFeatures(geojson));

    /// <summary>
    /// Produces a CSV string for an already-extracted feature list.
    /// </summary>
    public static string GenerateCsv(IReadOnlyList<JsonElement> features)
    {
        // Collect ordered set of all property keys
        var keySet = new List<string>();
        var keyIndex = new HashSet<string>(StringComparer.Ordinal);

        foreach (var feature in features)
        {
            if (!feature.TryGetProperty("properties", out var props) ||
                props.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var prop in props.EnumerateObject())
            {
                if (keyIndex.Add(prop.Name))
                    keySet.Add(prop.Name);
            }
        }

        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", keySet.Append("geometry").Select(EscapeField)));

        // Data rows
        foreach (var feature in features)
        {
            var propsObj = feature.TryGetProperty("properties", out var p) &&
                           p.ValueKind == JsonValueKind.Object
                ? (JsonElement?)p
                : null;

            var geometry = feature.TryGetProperty("geometry", out var g) ? (JsonElement?)g : null;

            var fields = keySet
                .Select(key =>
                {
                    if (propsObj is { } props &&
                        props.TryGetProperty(key, out var val) &&
                        val.ValueKind != JsonValueKind.Null)
                        return EscapeField(JsonValueToString(val));
                    return string.Empty;
                })
                .Append(EscapeField(WktConverter.ToWkt(geometry)))
                .ToList();

            sb.AppendLine(string.Join(",", fields));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Applies RFC-4180 CSV escaping to a single field value.
    /// </summary>
    public static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Contains(',') || value.Contains('"') ||
            value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string JsonValueToString(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => value.GetRawText(),
        };
}
