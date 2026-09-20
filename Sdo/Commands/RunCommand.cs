using Nbuild.Helpers;
using System.CommandLine;
using YamlLauncher;
using YamlLauncher.Logging;
using YamlLauncher.Models;
using YamlDotNet.Serialization;

namespace Sdo.Commands;

/// <summary>
/// Executes YAML Launcher workflow manifests.
/// </summary>
public sealed class RunCommand : Command
{
    public RunCommand(Option<bool> verboseOption)
        : base("run", "Execute a YAML Launcher workflow manifest")
    {
        var manifestOption = new Option<string>("--manifest")
        {
            Description = "Path to a YAML workflow manifest",
            Required = true
        };

        Add(manifestOption);
        Add(verboseOption);
        SetAction(async parseResult => await ExecuteAsync(
            parseResult.GetValue(manifestOption)!,
            parseResult.GetValue(verboseOption)));
    }

    private static async Task<int> ExecuteAsync(string manifestPath, bool verbose)
    {
        try
        {
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

            var effectiveVerbose = verbose || config.Execution?.Verbose == true;
            var executor = new StepExecutor(effectiveVerbose, new ConsoleLogger(effectiveVerbose));
            var variables = config.Variables ?? new Dictionary<string, string>();
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

    private sealed class ConsoleLogger(bool verbose) : ILogger
    {
        public bool IsVerbose { get; } = verbose;

        public void LogInfo(string message) => Console.WriteLine(message);

        public void LogWarning(string message) => Console.Error.WriteLine(message);

        public void LogError(string message) => Console.Error.WriteLine(message);

        public void LogVerbose(string message)
        {
            if (IsVerbose)
            {
                ConsoleHelper.WriteLine(message, ConsoleColor.Cyan);
            }
        }
    }
}