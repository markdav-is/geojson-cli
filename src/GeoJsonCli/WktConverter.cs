using System.Text.Json;

namespace GeoJsonCli;

/// <summary>
/// Converts GeoJSON geometry objects to Well-Known Text (WKT).
/// </summary>
public static class WktConverter
{
    public static string ToWkt(JsonElement? geometry)
    {
        if (geometry is null || geometry.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return string.Empty;

        var geom = geometry.Value;

        if (!geom.TryGetProperty("type", out var typeProp))
            return string.Empty;

        var type = typeProp.GetString();

        return type switch
        {
            "Point" when geom.TryGetProperty("coordinates", out var c)
                => $"POINT({CoordPair(c)})",

            "LineString" when geom.TryGetProperty("coordinates", out var c)
                => $"LINESTRING({CoordList(c)})",

            "Polygon" when geom.TryGetProperty("coordinates", out var c)
                => $"POLYGON({RingList(c)})",

            "MultiPoint" when geom.TryGetProperty("coordinates", out var c)
                => $"MULTIPOINT({string.Join(", ", c.EnumerateArray().Select(p => $"({CoordPair(p)})"))})",

            "MultiLineString" when geom.TryGetProperty("coordinates", out var c)
                => $"MULTILINESTRING({string.Join(", ", c.EnumerateArray().Select(ls => $"({CoordList(ls)})"))})",

            "MultiPolygon" when geom.TryGetProperty("coordinates", out var c)
                => $"MULTIPOLYGON({string.Join(", ", c.EnumerateArray().Select(p => $"({RingList(p)})"))})",

            "GeometryCollection" when geom.TryGetProperty("geometries", out var gs)
                => $"GEOMETRYCOLLECTION({string.Join(", ", gs.EnumerateArray().Select(g => ToWkt(g)))})",

            _ => string.Empty,
        };
    }

    private static string CoordPair(JsonElement coord)
    {
        // Use GetRawText() to preserve the original numeric representation from the JSON source.
        var parts = coord.EnumerateArray()
            .Take(2)
            .Select(v => v.GetRawText())
            .ToArray();
        return string.Join(" ", parts);
    }

    private static string CoordList(JsonElement coords) =>
        string.Join(", ", coords.EnumerateArray().Select(CoordPair));

    private static string RingList(JsonElement rings) =>
        string.Join(", ", rings.EnumerateArray().Select(r => $"({CoordList(r)})"));
}
