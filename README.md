# geojson-cli

A C# .NET 10 command-line tool that extracts the schema (as a DDL SQL file) and data (as a CSV file) from a GeoJSON file.

When a GeoJSON file contains features with **distinct property schemas**, the tool automatically detects the groups and writes a separate DDL + CSV pair for each one.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build

```powershell
dotnet build
```

## Usage

```
dotnet run --project src/GeoJsonCli -- <input> [options]
```

Or after publishing:

```powershell
dotnet publish src/GeoJsonCli -c Release
.\publish\geojson-cli <input> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `<input>` | Path to the input GeoJSON file |

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `-o, --output <dir>` | Output directory for generated files | `.` (current directory) |
| `-t, --table <name>` | Override the base SQL table name | Input filename without extension |
| `-V, --version` | Print the version number | |
| `-h, --help` | Show help | |

### Example — single schema

```powershell
dotnet run --project src/GeoJsonCli -- places.geojson -o .\output
```

Generates two files in `.\output\`:

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

### Example — multiple schemas (automatic split)

When a FeatureCollection mixes features with different property key sets the tool splits them automatically, one DDL + CSV pair per distinct schema.

```powershell
dotnet run --project src/GeoJsonCli -- data/Signs.geojson -o .\output
```

```
DDL written to: .\output\Signs_signid.sql
CSV written to: .\output\Signs_signid.csv
DDL written to: .\output\Signs_routeid.sql
CSV written to: .\output\Signs_routeid.csv
DDL written to: .\output\Signs_shape.sql
CSV written to: .\output\Signs_shape.csv
DDL written to: .\output\Signs_1.sql
CSV written to: .\output\Signs_1.csv
```

Each output file is named `{baseName}_{discriminator}` where the discriminator is the first property key that exists exclusively in that schema group. When a group has no exclusive key (its property set is a subset of another group's), a numeric suffix is used instead (`_1`, `_2`, …).

The `-t, --table` option overrides the base name used for all generated files:

```powershell
dotnet run --project src/GeoJsonCli -- data/Signs.geojson -o .\output -t sign_supports
# produces: sign_supports_signid.sql, sign_supports_routeid.sql, …
```

## Type Inference

Property types are inferred from the values found across all features in a group:

| Condition | SQL Type |
|-----------|----------|
| All non-null values are booleans | `BOOLEAN` |
| All non-null values are integers | `INTEGER` |
| All non-null values are numbers (including floats) | `DOUBLE PRECISION` |
| Otherwise | `TEXT` |

Geometry is always stored as `TEXT` in WKT format.

## Supported Geometry Types

`Point`, `LineString`, `Polygon`, `MultiPoint`, `MultiLineString`, `MultiPolygon`, `GeometryCollection`

## Tests

```powershell
dotnet test
