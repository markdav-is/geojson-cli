using System.Text.Json;
using GeoJsonCli;

namespace GeoJsonCli.Tests;

public class WktConverterTests
{
    [Fact]
    public void ToWkt_Point_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"Point","coordinates":[-73.9857,40.7484]}""");
        Assert.Equal("POINT(-73.9857 40.7484)", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_LineString_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"LineString","coordinates":[[-74.006,40.7128],[-73.9857,40.7484]]}""");
        Assert.Equal("LINESTRING(-74.006 40.7128, -73.9857 40.7484)", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_Polygon_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"Polygon","coordinates":[[[-74.006,40.7128],[-73.9857,40.7484],[-73.9442,40.7282],[-74.006,40.7128]]]}""");
        Assert.Equal("POLYGON((-74.006 40.7128, -73.9857 40.7484, -73.9442 40.7282, -74.006 40.7128))", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_MultiPoint_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"MultiPoint","coordinates":[[0,1],[2,3]]}""");
        Assert.Equal("MULTIPOINT((0 1), (2 3))", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_MultiLineString_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"MultiLineString","coordinates":[[[0,0],[1,1]],[[2,2],[3,3]]]}""");
        Assert.Equal("MULTILINESTRING((0 0, 1 1), (2 2, 3 3))", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_MultiPolygon_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"MultiPolygon","coordinates":[[[[0,0],[1,0],[1,1],[0,0]]]]}""");
        Assert.Equal("MULTIPOLYGON(((0 0, 1 0, 1 1, 0 0)))", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_GeometryCollection_ReturnsCorrectWkt()
    {
        var geom = ParseGeometry("""{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[0,1]}]}""");
        Assert.Equal("GEOMETRYCOLLECTION(POINT(0 1))", WktConverter.ToWkt(geom));
    }

    [Fact]
    public void ToWkt_NullGeometry_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, WktConverter.ToWkt(null));
    }

    [Fact]
    public void ToWkt_UnknownType_ReturnsEmptyString()
    {
        var geom = ParseGeometry("""{"type":"Unknown","coordinates":[]}""");
        Assert.Equal(string.Empty, WktConverter.ToWkt(geom));
    }

    private static JsonElement ParseGeometry(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
