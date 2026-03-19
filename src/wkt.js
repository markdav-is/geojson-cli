'use strict';

/**
 * Converts a GeoJSON geometry object to its Well-Known Text (WKT) representation.
 * @param {object|null} geometry - A GeoJSON geometry object.
 * @returns {string} The WKT string, or an empty string if geometry is null/undefined.
 */
function toWKT(geometry) {
  if (!geometry) return '';

  switch (geometry.type) {
    case 'Point':
      return `POINT(${coordPair(geometry.coordinates)})`;

    case 'LineString':
      return `LINESTRING(${geometry.coordinates.map(coordPair).join(', ')})`;

    case 'Polygon':
      return `POLYGON(${geometry.coordinates.map(ring).join(', ')})`;

    case 'MultiPoint':
      return `MULTIPOINT(${geometry.coordinates.map(c => `(${coordPair(c)})`).join(', ')})`;

    case 'MultiLineString':
      return `MULTILINESTRING(${geometry.coordinates.map(ls => `(${ls.map(coordPair).join(', ')})`).join(', ')})`;

    case 'MultiPolygon':
      return `MULTIPOLYGON(${geometry.coordinates.map(p => `(${p.map(ring).join(', ')})`).join(', ')})`;

    case 'GeometryCollection':
      return `GEOMETRYCOLLECTION(${geometry.geometries.map(toWKT).join(', ')})`;

    default:
      return '';
  }
}

function coordPair(c) {
  return c.slice(0, 2).join(' ');
}

function ring(coords) {
  return `(${coords.map(coordPair).join(', ')})`;
}

module.exports = { toWKT };
