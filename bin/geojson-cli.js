#!/usr/bin/env node
'use strict';

const { Command } = require('commander');
const fs = require('fs');
const path = require('path');
const { generateDDL } = require('../src/schema');
const { generateCSV } = require('../src/csv');

const program = new Command();

program
  .name('geojson-cli')
  .description('Extract schema (DDL) and data (CSV) from a GeoJSON file')
  .version('1.0.0')
  .argument('<input>', 'Path to the input GeoJSON file')
  .option('-o, --output <dir>', 'Output directory for generated files', '.')
  .option('-t, --table <name>', 'Override the SQL table name (defaults to input filename without extension)')
  .action((input, options) => {
    const inputPath = path.resolve(input);

    if (!fs.existsSync(inputPath)) {
      console.error(`Error: File not found: ${inputPath}`);
      process.exit(1);
    }

    let geojson;
    try {
      const raw = fs.readFileSync(inputPath, 'utf8');
      geojson = JSON.parse(raw);
    } catch (err) {
      console.error(`Error: Failed to parse GeoJSON: ${err.message}`);
      process.exit(1);
    }

    if (!geojson || !geojson.type) {
      console.error('Error: Invalid GeoJSON – missing "type" property');
      process.exit(1);
    }

    const baseName = path.basename(inputPath, path.extname(inputPath));
    const tableName = options.table || baseName;
    const outputDir = path.resolve(options.output);

    if (!fs.existsSync(outputDir)) {
      fs.mkdirSync(outputDir, { recursive: true });
    }

    // Write DDL
    const ddl = generateDDL(geojson, tableName);
    const ddlPath = path.join(outputDir, `${tableName}.sql`);
    fs.writeFileSync(ddlPath, ddl, 'utf8');
    console.log(`DDL written to: ${ddlPath}`);

    // Write CSV
    const csv = generateCSV(geojson);
    const csvPath = path.join(outputDir, `${tableName}.csv`);
    fs.writeFileSync(csvPath, csv, 'utf8');
    console.log(`CSV written to: ${csvPath}`);
  });

program.parse();
