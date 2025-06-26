using System.CommandLine;
using wi;

/// <summary>
/// Entry point for the Azure DevOps Work Item CLI utility.
/// Supports creating PBIs and child tasks in Azure DevOps from a services file.
/// </summary>
/// <remarks>
/// Usage:
///   wi --services <path> --parentId <id>
///   wi --services <path> --parentId <id> --childTaskOfPbiId <pbiId>
/// </remarks>

// Fix for CS7036: Provide a parseArgument delegate to the Option constructor
var servicesFileOption = new Option<string>(
   name: "--services",
   parseArgument: result => result.Tokens.Count > 0 ? result.Tokens[0].Value : throw new ArgumentException("A value is required for --services"),
   description: "Path to services.txt file",
   isDefault: false
)
{ IsRequired = true };
servicesFileOption.AddAlias("-s");

var parentIdOption = new Option<int>(
  name: "--parentId",
  parseArgument: result =>
  {
      if (result.Tokens.Count > 0)
      {
          var tokenValue = result.Tokens[0].Value;
          if (int.TryParse(tokenValue, out var value))
          {
              return value;
          }
          throw new ArgumentException("A valid integer is required for --parentId");
      }
      throw new ArgumentException("A valid integer is required for --parentId");
  },
  description: "Parent work item ID",
  isDefault: false
)
{ IsRequired = true };
parentIdOption.AddAlias("-p");

var rootCommand = new RootCommand("Create PBIs for services from a file");
var childTaskPbiIdOption = new Option<int?>(
    name: "--childTaskOfPbiId",
    description: "If set, creates a child task with the same title as the PBI with this ID"
);
childTaskPbiIdOption.AddAlias("-c");
rootCommand.AddOption(childTaskPbiIdOption);
rootCommand.AddOption(servicesFileOption);
rootCommand.AddOption(parentIdOption);

/// <summary>
/// Handler for creating PBIs and child tasks for each service in the file.
/// </summary>
/// <param name="servicesPath">Path to the services file.</param>
/// <param name="parentId">Parent work item ID.</param>
/// <param name="childTaskOfPbiId">Optional: PBI ID to create a child task for.</param>
rootCommand.SetHandler(async (string servicesPath, int parentId, int? childTaskOfPbiId) =>
{
    string organization = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORGANIZATION") ?? "https://dev.azure.com/nazh"; // Default organization
    string project = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PROJECT") ?? "Proto"; // Default project

    // Output the organization and project for debugging
    Console.WriteLine($"Organization: {organization}");
    Console.WriteLine($"Project: {project}");

    string? pat = Environment.GetEnvironmentVariable("PAT");
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.WriteLine("PAT environment variable is not set.");
        return;
    }

    var helper = new AzureDevOpsWorkItemHelper(organization, project);

    // If the child task option is set, just create that task and exit
    if (childTaskOfPbiId.HasValue)
    {
        await helper.CreateChildTaskWithSameTitleAsync(childTaskOfPbiId.Value);
        return;
    }

    // Read services file
    string[] services;
    try
    {
        services = await File.ReadAllLinesAsync(servicesPath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to read services file: {ex.Message}");
        return;
    }

    // Process each service: create PBI and child task
    foreach (var service in services)
    {
        if (string.IsNullOrWhiteSpace(service)) continue;

        var title = $"{service}: update pipeline to perform SCA";
        var pbiId = await helper.CreatePbiAsync(title, parentId);
        if (!pbiId.HasValue)
        {
            Console.WriteLine($"Failed to create PBI for service: {service}");
            continue;
        }

        await helper.CreateChildTaskWithSameTitleAsync(pbiId.Value);
    }
}, servicesFileOption, parentIdOption, childTaskPbiIdOption);

/// <summary>
/// Program entry point. Invokes the root command with the provided arguments.
/// </summary>
return await rootCommand.InvokeAsync(args);
