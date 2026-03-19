'use strict';

/**
 * Infers a SQL DDL column type from a list of sampled values.
 * @param {Array} values - Non-null sample values for this column.
 * @returns {string} SQL type string.
 */
function inferType(values) {
  if (values.length === 0) return 'TEXT';

  const allBooleans = values.every(v => typeof v === 'boolean');
  if (allBooleans) return 'BOOLEAN';

  const allNumbers = values.every(v => typeof v === 'number' && isFinite(v));
  if (allNumbers) {
    const allIntegers = values.every(v => Number.isInteger(v));
    return allIntegers ? 'INTEGER' : 'DOUBLE PRECISION';
  }

  return 'TEXT';
}

/**
 * Builds a map of { columnName -> sqlType } from a FeatureCollection's features.
 * Property order is preserved by insertion order (first feature seen first).
 * @param {Array} features - Array of GeoJSON Feature objects.
 * @returns {Map<string, string>} Ordered map of column names to SQL types.
 */
function inferColumns(features) {
  const columnValues = new Map();

  for (const feature of features) {
    const props = feature.properties || {};
    for (const [key, value] of Object.entries(props)) {
      if (!columnValues.has(key)) {
        columnValues.set(key, []);
      }
      if (value !== null && value !== undefined) {
        columnValues.get(key).push(value);
      }
    }
  }

  const columns = new Map();
  for (const [key, values] of columnValues) {
    columns.set(key, inferType(values));
  }
  return columns;
}

/**
 * Generates a CREATE TABLE DDL statement from a GeoJSON FeatureCollection.
 * @param {object} geojson - Parsed GeoJSON object (FeatureCollection or Feature).
 * @param {string} tableName - The name to use for the SQL table.
 * @returns {string} DDL SQL string.
 */
function generateDDL(geojson, tableName) {
  const features = getFeatures(geojson);
  const columns = inferColumns(features);

  const colDefs = [];

  for (const [name, type] of columns) {
    colDefs.push(`  ${quoteName(name)} ${type}`);
  }

  colDefs.push('  geometry TEXT');

  return `CREATE TABLE ${quoteName(tableName)} (\n${colDefs.join(',\n')}\n);\n`;
}

/**
 * Extracts the features array from a GeoJSON object.
 * @param {object} geojson
 * @returns {Array}
 */
function getFeatures(geojson) {
  if (geojson.type === 'FeatureCollection') {
    return geojson.features || [];
  }
  if (geojson.type === 'Feature') {
    return [geojson];
  }
  return [];
}

/**
 * Quotes an identifier for use in SQL (wraps in double quotes, escaping any
 * internal double quotes).
 * @param {string} name
 * @returns {string}
 */
function quoteName(name) {
  return `"${name.replace(/"/g, '""')}"`;
}

module.exports = { generateDDL, inferColumns, inferType, getFeatures };
