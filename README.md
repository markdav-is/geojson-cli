# geojson-cli

Command line tool to extract the schema and data from a GeoJSON file into DDL and CSVs.

## Usage

```
geojson-cli <input> [options]
```

## Arguments

| Argument | Description |
|----------|-------------|
| `input`  | Path to the GeoJSON file to process |

## Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--output <dir>` | `-o` | Output directory for generated files (defaults to input file directory) |
| `--table <name>` | `-t` | Table name for the DDL (defaults to the input filename without extension) |
| `--no-header` | | Omit the header row from CSV output |
| `--help` | `-h` | Show help and usage information |
| `--version` | | Show version information |

## Output

For a given input file, the tool generates:

- `<table>.sql` — CREATE TABLE DDL with inferred column types
- `<table>.csv` — Data rows exported from the GeoJSON feature properties

## Examples

```bash
# Basic usage — outputs files next to the input file
geojson-cli data/parks.geojson

# Specify output directory and table name
geojson-cli data/parks.geojson --output ./output --table parks

# Export CSV without header row
geojson-cli data/parks.geojson --no-header
```
