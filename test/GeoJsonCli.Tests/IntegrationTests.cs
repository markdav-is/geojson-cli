using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GeoJsonCli.Tests;

/// <summary>
/// End-to-end tests that run the published geojson-cli executable against real input files.
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.geojson");
    private static readonly string ExePath = GetExePath();

    public IntegrationTests()
    {
        Directory.CreateDirectory(_tmpDir);
    }

    [Fact]
    public void Cli_GeneratesDdlAndCsv_FromGeoJsonFixture()
    {
        var result = RunCli($"\"{FixturePath}\" -o \"{_tmpDir}\"");
        Assert.Equal(0, result.ExitCode);

        var ddlPath = Path.Combine(_tmpDir, "sample.sql");
        var csvPath = Path.Combine(_tmpDir, "sample.csv");

        Assert.True(File.Exists(ddlPath), "DDL file should exist");
        Assert.True(File.Exists(csvPath), "CSV file should exist");

        Assert.Contains("CREATE TABLE \"sample\"", File.ReadAllText(ddlPath));
        Assert.StartsWith("name,height,floors,area,landmark,geometry", File.ReadAllText(csvPath));
    }

    [Fact]
    public void Cli_TableOption_OverridesTableName()
    {
        var result = RunCli($"\"{FixturePath}\" -o \"{_tmpDir}\" --table locations");
        Assert.Equal(0, result.ExitCode);

        Assert.True(File.Exists(Path.Combine(_tmpDir, "locations.sql")));
        Assert.True(File.Exists(Path.Combine(_tmpDir, "locations.csv")));
        Assert.Contains("CREATE TABLE \"locations\"", File.ReadAllText(Path.Combine(_tmpDir, "locations.sql")));
    }

    [Fact]
    public void Cli_MissingInputFile_ExitsWithError()
    {
        var result = RunCli("/nonexistent/file.geojson");
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Error", result.StdErr);
    }

    [Fact]
    public void Cli_NoArguments_ExitsWithError()
    {
        var result = RunCli(string.Empty);
        Assert.NotEqual(0, result.ExitCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    // ---------------------------------------------------------------------------

    private static (int ExitCode, string StdOut, string StdErr) RunCli(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{ExePath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdout, stderr);
    }

    private static string GetExePath()
    {
        // The DLL lives next to this test assembly in the output directory.
        var dir = AppContext.BaseDirectory;
        return Path.Combine(dir, "..", "..", "..", "..", "..", "src", "GeoJsonCli", "bin", "Debug", "net10.0", "GeoJsonCli.dll");
    }
}
