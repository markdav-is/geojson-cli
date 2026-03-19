using System.Text.Json;

namespace GeoJsonCli;

public static class SchemaInferrer
{
    public static TableSchema Infer(string tableName, GeoJsonFeatureCollection featureCollection)
    {
        // Collect all property keys and their observed types across all features
        var columnTypes = new Dictionary<string, ColumnType>(StringComparer.OrdinalIgnoreCase);
        var nullableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allKeys = new LinkedList<string>(); // preserve first-seen order
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool hasGeometry = featureCollection.Features.Any(f => f.Geometry != null);

        foreach (var feature in featureCollection.Features)
        {
            if (feature.Properties == null)
                continue;

            foreach (var (key, value) in feature.Properties)
            {
                if (!seenKeys.Contains(key))
                {
                    seenKeys.Add(key);
                    allKeys.AddLast(key);
                }

                var inferredType = InferType(value);

                if (columnTypes.TryGetValue(key, out var existingType))
                {
                    columnTypes[key] = MergeTypes(existingType, inferredType);
                }
                else
                {
                    columnTypes[key] = inferredType;
                }
            }
        }

        // Mark columns nullable if any feature is missing the property or has a null value
        foreach (var feature in featureCollection.Features)
        {
            foreach (var key in seenKeys)
            {
                if (feature.Properties == null ||
                    !feature.Properties.TryGetValue(key, out var val) ||
                    val.ValueKind == JsonValueKind.Null)
                {
                    nullableColumns.Add(key);
                }
            }
        }

        var columns = new List<ColumnSchema>();

        // Add property columns in the order they were first seen
        foreach (var key in allKeys)
        {
            columns.Add(new ColumnSchema
            {
                Name = key,
                Type = columnTypes.TryGetValue(key, out var t) ? t : ColumnType.Text,
                Nullable = nullableColumns.Contains(key)
            });
        }

        // Add geometry column last if any feature has geometry
        if (hasGeometry)
        {
            columns.Add(new ColumnSchema
            {
                Name = "geometry",
                Type = ColumnType.Text,
                Nullable = featureCollection.Features.Any(f => f.Geometry == null)
            });
        }

        return new TableSchema { TableName = tableName, Columns = columns };
    }

    private static ColumnType InferType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True or JsonValueKind.False => ColumnType.Boolean,
            JsonValueKind.Number => IsInteger(element) ? ColumnType.Integer : ColumnType.Real,
            _ => ColumnType.Text
        };
    }

    private static bool IsInteger(JsonElement element)
    {
        if (element.TryGetInt64(out _))
            return true;
        return false;
    }

    // When a column has conflicting observed types, widen to the more general type.
    // Hierarchy: Boolean < Integer < Real < Text
    private static ColumnType MergeTypes(ColumnType a, ColumnType b)
    {
        if (a == b) return a;
        if (a == ColumnType.Text || b == ColumnType.Text) return ColumnType.Text;
        if (a == ColumnType.Real || b == ColumnType.Real) return ColumnType.Real;
        if (a == ColumnType.Integer || b == ColumnType.Integer) return ColumnType.Integer;
        return ColumnType.Text;
    }
}
