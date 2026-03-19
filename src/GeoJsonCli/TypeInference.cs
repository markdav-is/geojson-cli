using System.Text.Json;

namespace GeoJsonCli;

/// <summary>
/// Infers SQL column types from GeoJSON property values.
/// </summary>
public static class TypeInference
{
    /// <summary>
    /// Returns the SQL type string for a collection of sampled (non-null) JSON values.
    /// </summary>
    public static string InferSqlType(IReadOnlyList<JsonElement> values)
    {
        if (values.Count == 0)
            return "TEXT";

        if (values.All(v => v.ValueKind is JsonValueKind.True or JsonValueKind.False))
            return "BOOLEAN";

        if (values.All(v => v.ValueKind == JsonValueKind.Number))
        {
            if (values.All(v => v.TryGetInt64(out _)))
                return "INTEGER";
            return "DOUBLE PRECISION";
        }

        return "TEXT";
    }

    /// <summary>
    /// Scans all features and returns an ordered list of (columnName, sqlType) pairs.
    /// Column order follows the first-seen order across all features.
    /// </summary>
    public static IReadOnlyList<(string Name, string Type)> InferColumns(IEnumerable<JsonElement> features)
    {
        var order = new List<string>();
        var columnValues = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);

        foreach (var feature in features)
        {
            if (!feature.TryGetProperty("properties", out var props) ||
                props.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var prop in props.EnumerateObject())
            {
                if (!columnValues.ContainsKey(prop.Name))
                {
                    columnValues[prop.Name] = [];
                    order.Add(prop.Name);
                }

                if (prop.Value.ValueKind != JsonValueKind.Null)
                    columnValues[prop.Name].Add(prop.Value);
            }
        }

        return order
            .Select(name => (name, InferSqlType(columnValues[name])))
            .ToList();
    }
}
