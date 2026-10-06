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
    public void Main_WithUnmatchedTargetWithoutBuildFile_ReturnsBuildFailure()
    {
        var result = Program.Main($"missing-target-{Guid.NewGuid():N}");

        Assert.Equal(-1, result);
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
    public void Main_WithRepositoryInfoCommand_ReturnsZero()
    {
        var result = Program.Main("repo", "info", "--dry-run");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithRepositoryInfoDryRun_ReturnsZero()
    {
        var result = Program.Main("repo", "info", "--dry-run");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithRepositoryCloneDryRun_ReturnsZero()
    {
        var result = Program.Main(
            "repo",
            "clone",
            "--url",
            "https://github.com/user/repo",
            "--path",
            "C:\\temp\\repo",
            "--dry-run");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithRepositoryTagDryRunCommands_ReturnZero()
    {
        var setResult = Program.Main("repo", "tag", "set", "--tag", "1.2.3", "--dry-run");
        var deleteResult = Program.Main("repo", "tag", "delete", "--tag", "1.2.3", "--dry-run");
        var pushAutoResult = Program.Main("repo", "tag", "push-auto", "--buildtype", "stage", "--dry-run");

        Assert.Equal(0, setResult);
        Assert.Equal(0, deleteResult);
        Assert.Equal(0, pushAutoResult);
    }

    [Fact]
    public void Main_WithBuildTypeOnRepositoryTagSet_ReturnsNonZero()
    {
        var result = Program.Main(
            "repo", "tag", "set",
            "--tag", "push-auto",
            "--buildtype", "stage",
            "--dry-run");

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithToolListInvalidManifest_ReturnsNonZero()
    {
        var result = Program.Main("tool", "list", "--json", "missing-apps.json");

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithToolManifestOnRun_ReturnsNonZero()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-apps-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(manifestPath, "Version: '1.0'\nDownloadPath: 'C:/downloads'\nNbuildAppList: []\n");

        try
        {
            var result = Program.Main("run", "--manifest", manifestPath);

            Assert.NotEqual(0, result);
        }
        finally
        {
            File.Delete(manifestPath);
        }
    }

    [Fact]
    public void Main_WithMissingManifestPath_ReturnsNonZero()
    {
        var result = Program.Main("run", "--manifest", "--stage", "stage");

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithoutManifest_UsesSdoYamlInCurrentDirectory()
    {
        var originalDirectory = Environment.CurrentDirectory;
        var testDirectory = Path.Combine(Path.GetTempPath(), $"sdo-default-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
        Environment.CurrentDirectory = testDirectory;
        File.WriteAllText(Path.Combine(testDirectory, "sdo.yaml"), """
version: '1.0'
steps:
  - name: default
    path: cmd.exe
    arguments: /c exit 0
""");

        try
        {
            var result = Program.Main("run");

            Assert.Equal(0, result);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void Main_WithStopOnFirstErrorFalse_ContinuesAfterFailure()
    {
        var manifestPath = CreateFailureManifest(stopOnFirstError: false, includeExecution: true, out var markerPath);

        try
        {
            var result = Program.Main("run", "--manifest", manifestPath);

            Assert.Equal(1, result);
            Assert.True(File.Exists(markerPath));
        }
        finally
        {
            DeleteFiles(manifestPath, markerPath);
        }
    }

    [Fact]
    public void Main_WithStopOnFirstErrorTrue_StopsAfterFailure()
    {
        var manifestPath = CreateFailureManifest(stopOnFirstError: true, includeExecution: true, out var markerPath);

        try
        {
            var result = Program.Main("run", "--manifest", manifestPath);

            Assert.Equal(1, result);
            Assert.False(File.Exists(markerPath));
        }
        finally
        {
            DeleteFiles(manifestPath, markerPath);
        }
    }

    [Fact]
    public void Main_WithoutStopOnFirstError_StopsRegardlessOfExecutionSection()
    {
        var manifestWithoutExecution = CreateFailureManifest(null, includeExecution: false, out var markerWithoutExecution);
        var manifestWithExecution = CreateFailureManifest(null, includeExecution: true, out var markerWithExecution);

        try
        {
            Assert.Equal(1, Program.Main("run", "--manifest", manifestWithoutExecution));
            Assert.Equal(1, Program.Main("run", "--manifest", manifestWithExecution));
            Assert.False(File.Exists(markerWithoutExecution));
            Assert.False(File.Exists(markerWithExecution));
        }
        finally
        {
            DeleteFiles(manifestWithoutExecution, markerWithoutExecution, manifestWithExecution, markerWithExecution);
        }
    }

    [Fact]
    public void Main_WithNamedStage_ExecutesReferencedStepsInStageOrder()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-stage-{Guid.NewGuid():N}.yaml");
        var markerPath = Path.Combine(Path.GetTempPath(), $"sdo-stage-marker-{Guid.NewGuid():N}.txt").Replace('\\', '/');
        File.WriteAllText(manifestPath, $"""
version: '1.0'
steps:
  - name: first
    path: cmd.exe
    arguments: /c echo first>>{markerPath}
  - name: second
    path: cmd.exe
    arguments: /c echo second>>{markerPath}
stages:
  - name: release
    steps:
      - second
      - first
""");

        try
        {
            var result = Program.Main("run", "--manifest", manifestPath, "--stage", "release");

            Assert.Equal(0, result);
            Assert.Equal(["second", "first"], File.ReadAllLines(markerPath));
        }
        finally
        {
            DeleteFiles(manifestPath, markerPath);
        }
    }

    [Fact]
    public void Main_WithStepIndices_ExecutesRequestedOrder()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-index-{Guid.NewGuid():N}.yaml");
        var markerPath = Path.Combine(Path.GetTempPath(), $"sdo-index-marker-{Guid.NewGuid():N}.txt").Replace('\\', '/');
        File.WriteAllText(manifestPath, $"""
version: '1.0'
steps:
  - name: first
    path: cmd.exe
    arguments: /c echo first>>{markerPath}
  - name: second
    path: cmd.exe
    arguments: /c echo second>>{markerPath}
""");

        try
        {
            var result = Program.Main(
                "run", "--manifest", manifestPath,
                "--step-index", "1", "0");

            Assert.Equal(0, result);
            Assert.Equal(["second", "first"], File.ReadAllLines(markerPath));
        }
        finally
        {
            DeleteFiles(manifestPath, markerPath);
        }
    }

    [Fact]
    public void Main_WithNamedStep_ExecutesOnlyThatStep()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-step-{Guid.NewGuid():N}.yaml");
        var markerPath = Path.Combine(Path.GetTempPath(), $"sdo-step-marker-{Guid.NewGuid():N}.txt").Replace('\\', '/');
        File.WriteAllText(manifestPath, $"""
version: '1.0'
steps:
  - name: first
    path: cmd.exe
    arguments: /c echo first>>{markerPath}
  - name: selected
    path: cmd.exe
    arguments: /c echo selected>>{markerPath}
""");

        try
        {
            var result = Program.Main("run", "--manifest", manifestPath, "--step", "selected");

            Assert.Equal(0, result);
            Assert.Equal(["selected"], File.ReadAllLines(markerPath));
        }
        finally
        {
            DeleteFiles(manifestPath, markerPath);
        }
    }

    [Fact]
    public void Main_WithUnknownStage_ReturnsNonZeroBeforeExecutingSteps()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-invalid-stage-{Guid.NewGuid():N}.yaml");
        var markerPath = Path.Combine(Path.GetTempPath(), $"sdo-invalid-stage-marker-{Guid.NewGuid():N}.txt").Replace('\\', '/');
        File.WriteAllText(manifestPath, $"""
version: '1.0'
steps:
  - name: build
    path: cmd.exe
    arguments: /c echo build>>{markerPath}
""");

        try
        {
            var result = Program.Main("run", "--manifest", manifestPath, "--stage", "missing");

            Assert.NotEqual(0, result);
            Assert.False(File.Exists(markerPath));
        }
        finally
        {
            DeleteFiles(manifestPath, markerPath);
        }
    }

    [Fact]
    public void Main_WithMultipleRunSelections_ReturnsNonZero()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-selection-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(manifestPath, """
version: '1.0'
steps:
  - name: build
    path: cmd.exe
    arguments: /c exit 0
stages:
  - name: release
    steps:
      - build
""");

        try
        {
            var result = Program.Main(
                "run", "--manifest", manifestPath,
                "--stage", "release", "--step", "build");

            Assert.NotEqual(0, result);
        }
        finally
        {
            DeleteFiles(manifestPath);
        }
    }

    private static string CreateFailureManifest(bool? stopOnFirstError, bool includeExecution, out string markerPath)
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-run-{Guid.NewGuid():N}.yaml");
        markerPath = Path.Combine(Path.GetTempPath(), $"sdo-marker-{Guid.NewGuid():N}.txt").Replace('\\', '/');
        var executionSection = includeExecution
            ? $"execution:\n  verbose: false\n  stopOnFirstError: {(stopOnFirstError ?? true).ToString().ToLowerInvariant()}\n"
            : string.Empty;
        var yaml = $"""
version: '1.0'
{executionSection}steps:
  - name: fail
    path: cmd.exe
    arguments: /c exit 1
  - name: marker
    path: cmd.exe
    arguments: /c echo ran > {markerPath}
""";

        File.WriteAllText(manifestPath, yaml);
        return manifestPath;
    }

    private static void DeleteFiles(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Main_WithTestMetadataFile_RunsSharedLauncherRunner()
    {
        var metadataPath = Path.Combine(Path.GetTempPath(), $"sdo-test-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(metadataPath, """
version: '1.0'
steps:
  - name: test
    path: cmd.exe
    arguments: /c echo metadata
    assertions:
      - type: output_contains
        value: metadata
""");

        try
        {
            var result = Program.Main("e2e", "--test-case", metadataPath);

            Assert.Equal(0, result);
        }
        finally
        {
            File.Delete(metadataPath);
        }
    }

    [Fact]
    public void Main_WithWorkflowMetadataOnTest_ReturnsNonZero()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"sdo-workflow-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(manifestPath, """
version: '1.0'
steps:
  - name: build
    path: cmd.exe
    arguments: /c exit 0
""");

        try
        {
            var result = Program.Main("e2e", "--test-case", manifestPath);

            Assert.NotEqual(0, result);
        }
        finally
        {
            File.Delete(manifestPath);
        }
    }

    [Fact]
    public void Main_WithTestMetadataDirectory_RunsDiscoveredTests()
    {
        var metadataPath = Path.Combine(Path.GetTempPath(), $"sdo-metadata-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataPath);
        File.WriteAllText(Path.Combine(metadataPath, "Test_Discovered.yaml"), """
version: '1.0'
steps:
  - name: test
    path: cmd.exe
    arguments: /c exit 0
    assertions:
      - type: exit_code
        value: '0'
""");

        try
        {
            var result = Program.Main("e2e", "--metadata-path", metadataPath);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(metadataPath, true);
        }
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

    [Fact]
    public void Main_WithReleaseListWithoutRepository_ReturnsNonZero()
    {
        var result = Program.Main("release", "list");

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Main_WithReleaseCreateDryRun_ReturnsZero()
    {
        var result = Program.Main(
            "release", "create",
            "--repo", "owner/repository",
            "--tag", "v1.0.0",
            "--branch", "main",
            "--file", "release.zip",
            "--dry-run");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithReleaseDownloadDryRun_ReturnsZero()
    {
        var result = Program.Main(
            "release", "download",
            "--repo", "owner/repository",
            "--tag", "v1.0.0",
            "--dry-run");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithToolInstallUninstallAndDownloadDryRun_ReturnZero()
    {
        var installResult = Program.Main("tool", "install", "--json", "missing-apps.json", "--dry-run");
        var uninstallResult = Program.Main("tool", "uninstall", "--json", "missing-apps.json", "--dry-run");
        var downloadResult = Program.Main("tool", "download", "--json", "missing-apps.json", "--dry-run");

        Assert.Equal(0, installResult);
        Assert.Equal(0, uninstallResult);
        Assert.Equal(0, downloadResult);
    }

    [Fact]
    public void Main_WithToolInstallNameShortAliasAndDryRun_ReturnsZero()
    {
        var result = Program.Main("tool", "install", "-n", "NonExistentApp", "-dr");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithBackupInitAndRunDryRun_ReturnZero()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"sdo-backup-{Guid.NewGuid():N}.json");

        try
        {
            var initResult = Program.Main("backup", "init", "--output", outputPath);
            var runResult = Program.Main("backup", "run", "--input", outputPath, "--dry-run");

            Assert.Equal(0, initResult);
            Assert.Equal(0, runResult);
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void Main_WithBackupDirectExecutionDryRun_ReturnsZero()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"sdo-backup-{Guid.NewGuid():N}.json");

        try
        {
            var initResult = Program.Main("backup", "init", "--output", outputPath);
            var runResult = Program.Main("backup", "-i", outputPath, "-v", "-dr");

            Assert.Equal(0, initResult);
            Assert.Equal(0, runResult);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}