using Xunit;
using Sdo;

namespace SdoTests;

/// <summary>
/// Tests for the Sdo CLI Program class
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Main_WithNoArgs_ReturnsNonZero()
    {
        // Act - No arguments should return error (1) because a command is required
        var result = Program.Main();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact(Skip = "System.CommandLine v2.0.2 limitation: --help returns 1 instead of 0")]
    public void Main_WithHelpOption_ReturnsZero()
    {
        // Act
        var result = Program.Main("--help");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithVersionOption_ReturnsZero()
    {
        // Act
        var result = Program.Main("--version");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithInvalidOption_ReturnsNonZero()
    {
        // Act
        var result = Program.Main("--invalid-option");

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithEnvironmentPathCommand_ReturnsZero()
    {
        var result = Program.Main("env", "path");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithBuildTargetsCommand_ReturnsZero()
    {
        var result = Program.Main("build", "targets");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithToolListInvalidManifest_ReturnsNonZero()
    {
        var result = Program.Main("tool", "list", "--json", "missing-apps.json");

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithFileCommands_SearchesFilesAndFolders()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(testRoot, "plans"));
        File.WriteAllText(Path.Combine(testRoot, "plan.md"), "test");

        var originalOutput = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var filesResult = Program.Main("file", "files", "-d", testRoot, "-e", ".md");
            var foldersResult = Program.Main("file", "folders", "-d", testRoot, "-n", "plans");

            Assert.Equal(0, filesResult);
            Assert.Equal(0, foldersResult);
            Assert.Contains("plan.md", output.ToString());
            Assert.Contains("plans", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Directory.Delete(testRoot, true);
        }
    }
}