using System.CommandLine;
using Nbuild.Helpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlLauncher.Models;
using YamlLauncher.TestRunners;

namespace Sdo.Commands;

/// <summary>
/// Executes ntools-launcher test metadata YAML.
/// </summary>
public sealed class TestCommand : Command
{
    public TestCommand(Option<bool> verboseOption)
        : base("test", "Run ntools-launcher YAML test metadata")
    {
        var testCaseOption = new Option<string?>("--test-case")
        {
            Description = "Test metadata file path or test name"
        };
        var metadataPathOption = new Option<string?>("--metadata-path")
        {
            Description = "Directory containing test metadata YAML files"
        };

        Add(testCaseOption);
        Add(metadataPathOption);
        Add(verboseOption);
        SetAction(async parseResult => await ExecuteAsync(
            parseResult.GetValue(testCaseOption),
            parseResult.GetValue(metadataPathOption),
            parseResult.GetValue(verboseOption)));
    }

    private static async Task<int> ExecuteAsync(string? testCase, string? metadataPath, bool verbose)
    {
        try
        {
            var resolvedMetadataPath = ResolveMetadataPath(metadataPath);
            if (string.IsNullOrWhiteSpace(testCase))
            {
                if (!Directory.Exists(resolvedMetadataPath))
                {
                    ConsoleHelper.WriteError($"Test metadata directory was not found: {resolvedMetadataPath}");
                    ConsoleHelper.WriteError("Use --metadata-path to specify the directory containing Test_*.yaml files.");
                    return 1;
                }

                var runner = new NtoolsLauncherTestRunner(verbose, metadataPath: resolvedMetadataPath);
                return await runner.RunAllTestsAsync() ? 0 : 1;
            }

            var testFile = ResolveTestFile(testCase, resolvedMetadataPath);
            if (testFile != null)
            {
                var classification = await ClassifyAsync(testFile);
                if (classification == ManifestKind.Tool)
                {
                    ConsoleHelper.WriteError(
                        $"'{testFile}' is an apps/tool manifest, not test metadata. Use 'sdo tool list --manifest \"{testFile}\"' or another tool operation.");
                    return 1;
                }

                if (classification != ManifestKind.TestMetadata)
                {
                    ConsoleHelper.WriteError(
                        $"'{testFile}' is a generic workflow, not test metadata. Use 'sdo run --manifest \"{testFile}\"'.");
                    return 1;
                }

                resolvedMetadataPath = Path.GetDirectoryName(testFile) ?? resolvedMetadataPath;
                testCase = GetTestName(testFile);
            }

            var testRunner = new NtoolsLauncherTestRunner(verbose, metadataPath: resolvedMetadataPath);
            return await testRunner.RunTestAsync(testCase) ? 0 : 1;
        }
        catch (Exception exception)
        {
            ConsoleHelper.WriteError($"Test run failed: {exception.Message}");
            return 1;
        }
    }

    private static string ResolveMetadataPath(string? metadataPath)
    {
        if (!string.IsNullOrWhiteSpace(metadataPath))
        {
            return Path.GetFullPath(metadataPath);
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "metadata"),
            Path.Combine(Directory.GetCurrentDirectory(), "sdo-e2e-test", "metadata"),
            Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? string.Empty, "sdo-e2e-test", "metadata"),
            Path.Combine(AppContext.BaseDirectory, "metadata")
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? Path.GetFullPath(candidates[0]);
    }

    private static string? ResolveTestFile(string testCase, string metadataPath)
    {
        if (File.Exists(testCase))
        {
            return Path.GetFullPath(testCase);
        }

        foreach (var extension in new[] { ".yaml", ".ntools.yml", ".yml" })
        {
            var candidate = Path.Combine(metadataPath, $"{testCase}{extension}");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GetTestName(string testFile)
    {
        var name = Path.GetFileName(testFile);
        if (name.EndsWith(".ntools.yml", StringComparison.OrdinalIgnoreCase))
        {
            return name[..^".ntools.yml".Length];
        }

        return Path.GetFileNameWithoutExtension(name);
    }

    private static async Task<ManifestKind> ClassifyAsync(string manifestPath)
    {
        var yaml = await File.ReadAllTextAsync(manifestPath);
        var root = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<Dictionary<string, object?>>(yaml);

        if (root == null || root.Count == 0)
        {
            throw new ArgumentException("Test metadata YAML is empty.");
        }

        if (root.Keys.Any(key => key.Equals("NbuildAppList", StringComparison.OrdinalIgnoreCase) ||
                                 key.Equals("DownloadPath", StringComparison.OrdinalIgnoreCase)))
        {
            return ManifestKind.Tool;
        }

        var config = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<LauncherConfig>(yaml);

        if (config?.Steps?.Any(step =>
                (step.Assertions?.Count ?? 0) > 0 ||
                (step.ExtractVariables?.Count ?? 0) > 0) == true)
        {
            return ManifestKind.TestMetadata;
        }

        return ManifestKind.Workflow;
    }

    private enum ManifestKind
    {
        Workflow,
        Tool,
        TestMetadata
    }
}