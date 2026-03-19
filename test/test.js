'use strict';

const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const path = require('path');
const fs = require('fs');
const os = require('os');

const { toWKT } = require('../src/wkt');
const { generateDDL, inferType, inferColumns } = require('../src/schema');
const { generateCSV, escapeField } = require('../src/csv');

// ---------------------------------------------------------------------------
// WKT conversion
// ---------------------------------------------------------------------------

describe('toWKT', () => {
  it('converts a Point geometry', () => {
    const g = { type: 'Point', coordinates: [-73.9857, 40.7484] };
    assert.equal(toWKT(g), 'POINT(-73.9857 40.7484)');
  });

  it('converts a LineString geometry', () => {
    const g = {
      type: 'LineString',
      coordinates: [[-74.006, 40.7128], [-73.9857, 40.7484]],
    };
    assert.equal(toWKT(g), 'LINESTRING(-74.006 40.7128, -73.9857 40.7484)');
  });

  it('converts a Polygon geometry', () => {
    const g = {
      type: 'Polygon',
      coordinates: [
        [[-74.006, 40.7128], [-73.9857, 40.7484], [-73.9442, 40.7282], [-74.006, 40.7128]],
      ],
    };
    assert.equal(
      toWKT(g),
      'POLYGON((-74.006 40.7128, -73.9857 40.7484, -73.9442 40.7282, -74.006 40.7128))',
    );
  });

  it('converts a MultiPoint geometry', () => {
    const g = { type: 'MultiPoint', coordinates: [[0, 1], [2, 3]] };
    assert.equal(toWKT(g), 'MULTIPOINT((0 1), (2 3))');
  });

  it('converts a MultiLineString geometry', () => {
    const g = {
      type: 'MultiLineString',
      coordinates: [[[0, 0], [1, 1]], [[2, 2], [3, 3]]],
    };
    assert.equal(toWKT(g), 'MULTILINESTRING((0 0, 1 1), (2 2, 3 3))');
  });

  it('converts a MultiPolygon geometry', () => {
    const g = {
      type: 'MultiPolygon',
      coordinates: [[[[0, 0], [1, 0], [1, 1], [0, 0]]]],
    };
    assert.equal(toWKT(g), 'MULTIPOLYGON(((0 0, 1 0, 1 1, 0 0)))');
  });

  it('returns empty string for null geometry', () => {
    assert.equal(toWKT(null), '');
  });

  it('returns empty string for unknown geometry type', () => {
    assert.equal(toWKT({ type: 'Unknown', coordinates: [] }), '');
  });
});

// ---------------------------------------------------------------------------
// Type inference
// ---------------------------------------------------------------------------

describe('inferType', () => {
  it('returns TEXT for empty values', () => {
    assert.equal(inferType([]), 'TEXT');
  });

  it('returns BOOLEAN for boolean values', () => {
    assert.equal(inferType([true, false, true]), 'BOOLEAN');
  });

  it('returns INTEGER for integer values', () => {
    assert.equal(inferType([1, 2, 3]), 'INTEGER');
  });

  it('returns DOUBLE PRECISION for float values', () => {
    assert.equal(inferType([1.5, 2.3]), 'DOUBLE PRECISION');
  });

  it('returns TEXT for mixed types', () => {
    assert.equal(inferType([1, 'hello']), 'TEXT');
  });

  it('returns TEXT for string values', () => {
    assert.equal(inferType(['a', 'b']), 'TEXT');
  });
});

// ---------------------------------------------------------------------------
// Column inference
// ---------------------------------------------------------------------------

describe('inferColumns', () => {
  it('infers columns from features', () => {
    const features = [
      { properties: { name: 'A', count: 1, active: true } },
      { properties: { name: 'B', count: 2, active: false } },
    ];
    const cols = inferColumns(features);
    assert.equal(cols.get('name'), 'TEXT');
    assert.equal(cols.get('count'), 'INTEGER');
    assert.equal(cols.get('active'), 'BOOLEAN');
  });

  it('handles null property values gracefully', () => {
    const features = [
      { properties: { name: null } },
      { properties: { name: 'hello' } },
    ];
    const cols = inferColumns(features);
    assert.equal(cols.get('name'), 'TEXT');
  });

  it('handles missing properties object', () => {
    const features = [{ properties: null }, { properties: { x: 1 } }];
    const cols = inferColumns(features);
    assert.equal(cols.get('x'), 'INTEGER');
  });
});

// ---------------------------------------------------------------------------
// DDL generation
// ---------------------------------------------------------------------------

describe('generateDDL', () => {
  const sampleGeoJSON = JSON.parse(
    fs.readFileSync(path.join(__dirname, 'fixtures', 'sample.geojson'), 'utf8'),
  );

  it('includes the table name', () => {
    const ddl = generateDDL(sampleGeoJSON, 'my_table');
    assert.match(ddl, /CREATE TABLE "my_table"/);
  });

  it('includes property columns', () => {
    const ddl = generateDDL(sampleGeoJSON, 'sample');
    assert.match(ddl, /"name" TEXT/);
    assert.match(ddl, /"floors" INTEGER/);
    assert.match(ddl, /"landmark" BOOLEAN/);
    assert.match(ddl, /"area" DOUBLE PRECISION/);
  });

  it('includes a geometry column', () => {
    const ddl = generateDDL(sampleGeoJSON, 'sample');
    assert.match(ddl, /geometry TEXT/);
  });

  it('ends with a semicolon', () => {
    const ddl = generateDDL(sampleGeoJSON, 'sample');
    assert.ok(ddl.trimEnd().endsWith(');'));
  });

  it('handles a single Feature input', () => {
    const feature = {
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [0, 0] },
      properties: { id: 1, label: 'x' },
    };
    const ddl = generateDDL(feature, 'pts');
    assert.match(ddl, /"id" INTEGER/);
    assert.match(ddl, /"label" TEXT/);
  });
});

// ---------------------------------------------------------------------------
// CSV field escaping
// ---------------------------------------------------------------------------

describe('escapeField', () => {
  it('returns empty string for null', () => {
    assert.equal(escapeField(null), '');
  });

  it('returns empty string for undefined', () => {
    assert.equal(escapeField(undefined), '');
  });

  it('does not quote plain values', () => {
    assert.equal(escapeField('hello'), 'hello');
    assert.equal(escapeField(42), '42');
    assert.equal(escapeField(true), 'true');
  });

  it('quotes values containing commas', () => {
    assert.equal(escapeField('a,b'), '"a,b"');
  });

  it('quotes and escapes values containing double quotes', () => {
    assert.equal(escapeField('say "hi"'), '"say ""hi"""');
  });

  it('quotes values containing newlines', () => {
    assert.equal(escapeField('line1\nline2'), '"line1\nline2"');
  });
});

// ---------------------------------------------------------------------------
// CSV generation
// ---------------------------------------------------------------------------

describe('generateCSV', () => {
  const sampleGeoJSON = JSON.parse(
    fs.readFileSync(path.join(__dirname, 'fixtures', 'sample.geojson'), 'utf8'),
  );

  it('has a header row with all property keys and geometry', () => {
    const csv = generateCSV(sampleGeoJSON);
    const lines = csv.split('\n');
    assert.equal(lines[0], 'name,height,floors,area,landmark,geometry');
  });

  it('has the correct number of data rows', () => {
    const csv = generateCSV(sampleGeoJSON);
    const lines = csv.trimEnd().split('\n');
    // 1 header + 3 features
    assert.equal(lines.length, 4);
  });

  it('includes WKT geometry in the last column', () => {
    const csv = generateCSV(sampleGeoJSON);
    assert.ok(csv.includes('POINT(-73.9857 40.7484)'));
    assert.ok(csv.includes('LINESTRING(-74.006 40.7128, -73.9857 40.7484)'));
    assert.ok(csv.includes('POLYGON('));
  });

  it('handles null property values as empty fields', () => {
    const csv = generateCSV(sampleGeoJSON);
    // "Lower Manhattan Triangle" has height=null → second data row, height column (index 1) empty
    const lines = csv.trimEnd().split('\n');
    const row2 = lines[2].split(',');
    assert.equal(row2[1], ''); // height is null → empty
  });
});

// ---------------------------------------------------------------------------
// End-to-end: CLI integration
// ---------------------------------------------------------------------------

describe('CLI end-to-end', () => {
  it('generates DDL and CSV files from a GeoJSON fixture', () => {
    const { execSync } = require('child_process');
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'geojson-cli-'));
    const input = path.join(__dirname, 'fixtures', 'sample.geojson');
    const binPath = path.join(__dirname, '..', 'bin', 'geojson-cli.js');

    execSync(`node ${binPath} ${input} -o ${tmpDir}`);

    const ddlPath = path.join(tmpDir, 'sample.sql');
    const csvPath = path.join(tmpDir, 'sample.csv');

    assert.ok(fs.existsSync(ddlPath), 'DDL file should exist');
    assert.ok(fs.existsSync(csvPath), 'CSV file should exist');

    const ddl = fs.readFileSync(ddlPath, 'utf8');
    assert.match(ddl, /CREATE TABLE "sample"/);

    const csv = fs.readFileSync(csvPath, 'utf8');
    assert.ok(csv.startsWith('name,height,floors,area,landmark,geometry'));

    fs.rmSync(tmpDir, { recursive: true });
  });

  it('supports --table to override the table name', () => {
    const { execSync } = require('child_process');
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'geojson-cli-'));
    const input = path.join(__dirname, 'fixtures', 'sample.geojson');
    const binPath = path.join(__dirname, '..', 'bin', 'geojson-cli.js');

    execSync(`node ${binPath} ${input} -o ${tmpDir} --table locations`);

    const ddlPath = path.join(tmpDir, 'locations.sql');
    const csvPath = path.join(tmpDir, 'locations.csv');

    assert.ok(fs.existsSync(ddlPath), 'DDL file should exist with overridden name');
    assert.ok(fs.existsSync(csvPath), 'CSV file should exist with overridden name');

    const ddl = fs.readFileSync(ddlPath, 'utf8');
    assert.match(ddl, /CREATE TABLE "locations"/);

    fs.rmSync(tmpDir, { recursive: true });
  });

  it('exits with an error for a missing file', () => {
    const { execSync } = require('child_process');
    const binPath = path.join(__dirname, '..', 'bin', 'geojson-cli.js');
    assert.throws(
      () => execSync(`node ${binPath} /nonexistent/file.geojson`, { stdio: 'pipe' }),
      /Error/,
    );
  });
});
