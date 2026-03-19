using System.Text.Json;

namespace GeoJsonCli;

/// <summary>
/// Splits a flat list of GeoJSON features into groups based on their property key schemas.
/// Features that share the exact same set of property key names are placed in the same group.
/// </summary>
public static class FeatureSplitter
{
    /// <summary>
    /// Groups features by their property key signature.
    /// Returns a single group when all features share the same schema.
    /// Returns multiple named groups when distinct schemas are detected,
    /// each named <paramref name="baseName"/>_&lt;firstExclusiveKey&gt;.
    /// </summary>
    public static IReadOnlyList<(string Name, IReadOnlyList<JsonElement> Features)> SplitBySchema(
        IReadOnlyList<JsonElement> features,
        string baseName)
    {
        // bucket features by their ordered property key set
        var buckets = new Dictionary<string, (List<string> Keys, List<JsonElement> Features)>(StringComparer.Ordinal);

        foreach (var feature in features)
        {
            var keys = GetPropertyKeys(feature);
            var signature = string.Join('\0', keys);

            if (!buckets.TryGetValue(signature, out var bucket))
            {
                bucket = (keys, []);
                buckets[signature] = bucket;
            }

            bucket.Features.Add(feature);
        }

        if (buckets.Count == 1)
            return [(baseName, buckets.Values.First().Features)];

        // build a set of ALL keys across every group so we can find exclusive ones
        var allGroupKeys = buckets.Values
            .Select(b => b.Keys.ToHashSet(StringComparer.Ordinal))
            .ToList();

        var result = new List<(string Name, IReadOnlyList<JsonElement> Features)>(buckets.Count);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int fallbackIndex = 1;

        foreach (var (_, (keys, groupFeatures)) in buckets)
        {
            var exclusiveKey = keys.FirstOrDefault(
                k => allGroupKeys.Count(g => g.Contains(k)) == 1);

            string suffix = exclusiveKey is not null
                ? exclusiveKey
                : (fallbackIndex++).ToString();

            // ensure uniqueness in the unlikely case two groups share the same exclusive key name
            var name = $"{baseName}_{suffix}";
            while (!usedNames.Add(name))
                name = $"{baseName}_{suffix}_{fallbackIndex++}";

            result.Add((name, groupFeatures));
        }

        return result;
    }

    private static List<string> GetPropertyKeys(JsonElement feature)
    {
        if (!feature.TryGetProperty("properties", out var props) ||
            props.ValueKind != JsonValueKind.Object)
            return [];

        return [.. props.EnumerateObject().Select(p => p.Name)];
    }
}
