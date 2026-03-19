'use strict';

const { toWKT } = require('./wkt');
const { getFeatures } = require('./schema');

/**
 * Escapes a single CSV field value.
 * Wraps in double quotes if the value contains a comma, double quote, or newline.
 * @param {*} value
 * @returns {string}
 */
function escapeField(value) {
  if (value === null || value === undefined) return '';
  const str = String(value);
  if (str.includes(',') || str.includes('"') || str.includes('\n') || str.includes('\r')) {
    return `"${str.replace(/"/g, '""')}"`;
  }
  return str;
}

/**
 * Generates CSV content from a GeoJSON object.
 * Columns are all property keys found across features, followed by a
 * `geometry` column containing the WKT representation.
 * @param {object} geojson - Parsed GeoJSON object.
 * @returns {string} CSV string.
 */
function generateCSV(geojson) {
  const features = getFeatures(geojson);

  // Collect ordered set of all property keys across all features
  const keySet = new Set();
  for (const feature of features) {
    for (const key of Object.keys(feature.properties || {})) {
      keySet.add(key);
    }
  }
  const keys = [...keySet];

  const header = [...keys, 'geometry'].map(escapeField).join(',');

  const rows = features.map(feature => {
    const props = feature.properties || {};
    const fields = keys.map(k => escapeField(props[k] ?? null));
    fields.push(escapeField(toWKT(feature.geometry)));
    return fields.join(',');
  });

  return [header, ...rows].join('\n') + '\n';
}

module.exports = { generateCSV, escapeField };
