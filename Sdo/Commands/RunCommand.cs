using Launcher.Services;
using System.CommandLine;
using YamlDotNet.Serialization;
using YamlLauncher;
using YamlLauncher.Models;

namespace Sdo.Commands;

/// <summary>
/// Executes YAML Launcher workflow manifests.
/// </summary>
public sealed class RunCommand : Command
{
    public RunCommand(Option<bool> verboseOption)
        : base("run", "Execute a YAML Launcher workflow manifest")
    {
        var manifestOption = new Option<string?>("--manifest")
        {
            Description = "Path to a YAML workflow manifest (default: sdo.yaml in the current directory)"
        };
        manifestOption.DefaultValueFactory = _ => "sdo.yaml";
        var stageOption = new Option<string?>("--stage")
        {
            Description = "Execute the named workflow stage"
        };
        var stepOption = new Option<string?>("--step")
        {
            Description = "Execute the uniquely named workflow step"
        };
        var stepIndexOption = new Option<int[]>("--step-index")
        {
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true,
            Description = "Execute workflow steps by zero-based index in the supplied order"
        };

        Add(manifestOption);
        Add(verboseOption);
        Add(stageOption);
        Add(stepOption);
        Add(stepIndexOption);
        SetAction(async parseResult => await ExecuteAsync(
            parseResult.GetValue(manifestOption)!,
            parseResult.GetValue(verboseOption),
            parseResult.GetValue(stageOption),
            parseResult.GetValue(stepOption),
            parseResult.GetValue(stepIndexOption)));
    }

    private static async Task<int> ExecuteAsync(
        string manifestPath,
        bool verbose,
        string? stageName,
        string? stepName,
        int[]? stepIndices)
    {
        try
        {
            if (manifestPath.StartsWith("-", StringComparison.Ordinal))
            {
                ConsoleHelper.WriteError(
                    "The --manifest option requires a workflow file path. Example: --manifest sdo.yaml --stage stage");
                return 1;
            }

            if (await IsToolManifestAsync(manifestPath))
            {
                ConsoleHelper.WriteError("The supplied manifest is an apps/tool manifest. Use 'sdo tool' for tool manifests; 'sdo run' expects workflow YAML with a steps collection.");
                return 1;
            }

            var loader = new YamlLauncherConfigLoader();
            var config = await loader.LoadFromFileAsync(manifestPath);
            if (config.Steps == null || config.Steps.Count == 0)
            {
                ConsoleHelper.WriteError("Workflow manifest must define at least one step.");
                return 1;
            }

            var selectionCount =
                (stageName != null ? 1 : 0) +
                (stepName != null ? 1 : 0) +
                (stepIndices is { Length: > 0 } ? 1 : 0);
            if (selectionCount > 1)
            {
                ConsoleHelper.WriteError("Specify only one of --stage, --step, or --step-index.");
                return 1;
            }

            var effectiveVerbose = verbose || config.Execution?.Verbose == true;
            var executor = new StepExecutor(effectiveVerbose);
            var variables = config.Variables ?? [];

            if (selectionCount == 1)
            {
                var expandedConfig = ExpandConfig(config, variables);
                var selectedResult = stageName != null
                    ? await executor.LaunchStageAsync(expandedConfig, stageName)
                    : stepName != null
                        ? await executor.LaunchAsync(expandedConfig, stepName)
                        : await executor.LaunchAsync(expandedConfig, stepIndices!);

                PrintOutput(selectedResult.Results);
                var selectedCount = selectedResult.Results?.Count ?? 0;
                var selectedSummary = $"Steps complete: {selectedCount}/{selectedCount}";
                if (selectedResult.Success)
                {
                    ConsoleHelper.WriteSuccess(selectedSummary);
                }
                else
                {
                    ConsoleHelper.WriteError(selectedSummary);
                }

                return selectedResult.Success ? 0 : 1;
            }

            var completedSteps = 0;
            var success = true;

            foreach (var configuredStep in config.Steps)
            {
                var step = ExpandStep(configuredStep, variables);
                var stepResult = await executor.LaunchAsync(
                    new LauncherConfig
                    {
                        Version = config.Version,
                        Description = config.Description,
                        Execution = config.Execution,
                        Variables = config.Variables,
                        Steps = [step]
                    },
                    0);

                PrintOutput(stepResult.Results);
                completedSteps++;
                if (!stepResult.Success)
                {
                    success = false;
                    if (config.Execution?.StopOnFirstError != false)
                    {
                        break;
                    }
                }
            }

            var summary = $"Steps complete: {completedSteps}/{config.Steps.Count}";
            if (success)
            {
                ConsoleHelper.WriteSuccess(summary);
            }
            else
            {
                ConsoleHelper.WriteError(summary);
            }

            return success ? 0 : 1;
        }
        catch (Exception exception)
        {
            ConsoleHelper.WriteError($"Workflow run failed: {exception.Message}");
            return 1;
        }
    }

    private static LauncherConfig ExpandConfig(
        LauncherConfig config,
        IReadOnlyDictionary<string, string> variables)
    {
        return new LauncherConfig
        {
            Version = config.Version,
            Description = config.Description,
            Execution = config.Execution,
            Variables = config.Variables,
            Stages = config.Stages,
            Steps = [.. config.Steps!.Select(step => ExpandStep(step, variables))]
        };
    }

    private static async Task<bool> IsToolManifestAsync(string manifestPath)
    {
        var yaml = await File.ReadAllTextAsync(manifestPath);
        var document = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<Dictionary<string, object?>>(yaml);

        return document.Keys.Any(key =>
            key.Equals("NbuildAppList", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("DownloadPath", StringComparison.OrdinalIgnoreCase));
    }

    private static StepConfig ExpandStep(
        StepConfig step,
        IReadOnlyDictionary<string, string> variables)
    {
        return new StepConfig
        {
            Name = Expand(step.Name, variables),
            Path = Expand(step.Path, variables),
            Arguments = Expand(step.Arguments, variables),
            Dependencies = step.Dependencies,
            ContinueOnError = step.ContinueOnError,
            ExpectedReturnCode = step.ExpectedReturnCode,
            Assertions = step.Assertions,
            ExtractVariables = step.ExtractVariables,
            WorkingDirectory = Expand(step.WorkingDirectory, variables),
            Environment = step.Environment
        };
    }

    private static string? Expand(string? value, IReadOnlyDictionary<string, string> variables)
    {
        if (value == null)
        {
            return null;
        }

        foreach (var variable in variables)
        {
            value = value.Replace($"$({variable.Key})", variable.Value, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private static void PrintOutput(List<ExecutionResult>? results)
    {
        foreach (var result in results ?? [])
        {
            if (!string.IsNullOrWhiteSpace(result.StdOut))
            {
                Console.WriteLine(result.StdOut);
            }

            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                Console.Error.WriteLine(result.StdErr);
            }
        }
    }
}