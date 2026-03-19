using System.Text;
using System.Text.Json;

namespace GeoJsonCli;

public static class CsvExporter
{
    public static void Export(TableSchema schema, GeoJsonFeatureCollection featureCollection, string outputPath)
    {
        using var writer = new StreamWriter(outputPath, append: false, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        // Header row
        writer.WriteLine(string.Join(",", schema.Columns.Select(c => EscapeCsvField(c.Name))));

        // Data rows
        foreach (var feature in featureCollection.Features)
        {
            var fields = new List<string>();

            foreach (var col in schema.Columns)
            {
                if (col.Name == "geometry")
                {
                    fields.Add(feature.Geometry != null
                        ? EscapeCsvField(GeometryToWkt(feature.Geometry))
                        : "");
                    continue;
                }

                if (feature.Properties == null || !feature.Properties.TryGetValue(col.Name, out var value))
                {
                    fields.Add("");
                    continue;
                }

                fields.Add(EscapeCsvField(JsonElementToString(value)));
            }

            writer.WriteLine(string.Join(",", fields));
        }
    }

    private static string JsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "",
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.Object or JsonValueKind.Array => element.GetRawText(),
            _ => element.ToString()
        };
    }

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string GeometryToWkt(GeoJsonGeometry geometry)
    {
        try
        {
            return geometry.Type.ToUpperInvariant() switch
            {
                "POINT" => PointToWkt(geometry.Coordinates),
                "MULTIPOINT" => MultiPointToWkt(geometry.Coordinates),
                "LINESTRING" => LineStringToWkt(geometry.Coordinates),
                "MULTILINESTRING" => MultiLineStringToWkt(geometry.Coordinates),
                "POLYGON" => PolygonToWkt(geometry.Coordinates),
                "MULTIPOLYGON" => MultiPolygonToWkt(geometry.Coordinates),
                _ => geometry.Coordinates.GetRawText()
            };
        }
        catch
        {
            // Fallback to raw JSON if WKT conversion fails
            return geometry.Coordinates.GetRawText();
        }
    }

    private static string PointToWkt(JsonElement coords)
    {
        var (x, y, z) = GetXYZ(coords);
        return z != null ? $"POINT Z ({x} {y} {z})" : $"POINT ({x} {y})";
    }

    private static string MultiPointToWkt(JsonElement coords)
    {
        var points = coords.EnumerateArray().Select(c => CoordPairToWkt(c));
        return $"MULTIPOINT ({string.Join(", ", points)})";
    }

    private static string LineStringToWkt(JsonElement coords)
    {
        var points = coords.EnumerateArray().Select(c => CoordPairToWkt(c));
        return $"LINESTRING ({string.Join(", ", points)})";
    }

    private static string MultiLineStringToWkt(JsonElement coords)
    {
        var lines = coords.EnumerateArray()
            .Select(line => $"({string.Join(", ", line.EnumerateArray().Select(c => CoordPairToWkt(c)))})");
        return $"MULTILINESTRING ({string.Join(", ", lines)})";
    }

    private static string PolygonToWkt(JsonElement coords)
    {
        var rings = coords.EnumerateArray()
            .Select(ring => $"({string.Join(", ", ring.EnumerateArray().Select(c => CoordPairToWkt(c)))})");
        return $"POLYGON ({string.Join(", ", rings)})";
    }

    private static string MultiPolygonToWkt(JsonElement coords)
    {
        var polygons = coords.EnumerateArray()
            .Select(poly =>
            {
                var rings = poly.EnumerateArray()
                    .Select(ring => $"({string.Join(", ", ring.EnumerateArray().Select(c => CoordPairToWkt(c)))})");
                return $"({string.Join(", ", rings)})";
            });
        return $"MULTIPOLYGON ({string.Join(", ", polygons)})";
    }

    private static string CoordPairToWkt(JsonElement coord)
    {
        var (x, y, z) = GetXYZ(coord);
        return z != null ? $"{x} {y} {z}" : $"{x} {y}";
    }

    private static (string x, string y, string? z) GetXYZ(JsonElement coord)
    {
        var arr = coord.EnumerateArray().ToList();
        var x = arr[0].GetRawText();
        var y = arr[1].GetRawText();
        string? z = arr.Count > 2 ? arr[2].GetRawText() : null;
        return (x, y, z);
    }
}
