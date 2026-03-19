# geojson-cli

A command-line tool to extract the schema (as a DDL SQL file) and data (as a CSV file) from a GeoJSON file.

## Installation

```bash
npm install -g geojson-cli
```

Or run directly from the repository:

```bash
npm install
```

## Usage

```
geojson-cli <input> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `<input>` | Path to the input GeoJSON file |

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `-o, --output <dir>` | Output directory for generated files | `.` (current directory) |
| `-t, --table <name>` | Override the SQL table name | Input filename without extension |
| `-V, --version` | Output the version number | |
| `-h, --help` | Display help | |

### Example

```bash
geojson-cli places.geojson -o ./output
```

This generates two files in `./output/`:

- **`places.sql`** — A `CREATE TABLE` DDL statement with column types inferred from the GeoJSON properties.
- **`places.csv`** — A CSV file with one row per GeoJSON feature; property values plus a `geometry` column in [WKT](https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry) format.

#### Sample DDL output (`places.sql`)

```sql
CREATE TABLE "places" (
  "name" TEXT,
  "population" INTEGER,
  "area" DOUBLE PRECISION,
  "capital" BOOLEAN,
  geometry TEXT
);
```

#### Sample CSV output (`places.csv`)

```csv
name,population,area,capital,geometry
New York,8336817,783.8,false,POINT(-74.006 40.7128)
Washington,689545,177.0,true,POINT(-77.0369 38.9072)
```

## Type Inference

Property types are inferred from the values found across all features:

| Condition | SQL Type |
|-----------|----------|
| All non-null values are booleans | `BOOLEAN` |
| All non-null values are integers | `INTEGER` |
| All non-null values are numbers (including floats) | `DOUBLE PRECISION` |
| Otherwise | `TEXT` |

Geometry is always stored as `TEXT` in WKT format.

## Supported Geometry Types

`Point`, `LineString`, `Polygon`, `MultiPoint`, `MultiLineString`, `MultiPolygon`, `GeometryCollection`

## Development

```bash
npm test
```
