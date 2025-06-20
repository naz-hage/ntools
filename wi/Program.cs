using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
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
rootCommand.SetHandler(async (string servicesPath, int parentId) =>
{
    string organization = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORGANIZATION") ?? "https://dev.azure.com/nazh"; // Default organization
    string project = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PROJECT") ?? "Proto"; // Default project

    // output the organization and project for debugging
    Console.WriteLine($"Organization: {organization}");
    Console.WriteLine($"Project: {project}");
    string? pat = Environment.GetEnvironmentVariable("PAT")!;
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.WriteLine("PAT environment variable is not set.");
        return;
    }

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

    using var client = new HttpClient();
    var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

    var helper = new AzureDevOpsWorkItemHelper(organization, project);

    foreach (var service in services)
    {
        if (string.IsNullOrWhiteSpace(service)) continue;
        var title = $"{service}: update pipeline to perform SCA";
        var pbiId = await helper.CreatePbiAsync(title, parentId);
        if (!pbiId.HasValue || string.IsNullOrWhiteSpace(pbiId.ToString()))
        {
            Console.WriteLine($"Failed to create PBI for service: {service}");
            continue;
        }

        // Optionally, create a child task with the same title  
        await helper.CreateChildTaskWithSameTitleAsync(pbiId.Value);

    }

}, servicesFileOption, parentIdOption);

/// <summary>
/// Handler for creating a child task for a specific PBI, or PBIs for all services.
/// </summary>
/// <param name="servicesPath">Path to the services file.</param>
/// <param name="parentId">Parent work item ID.</param>
/// <param name="childTaskOfPbiId">Optional: PBI ID to create a child task for.</param>
rootCommand.SetHandler(async (string servicesPath, int parentId, int? childTaskOfPbiId) =>
{
    string organization = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORGANIZATION") ?? "https://dev.azure.com/nazh"; // Default organization
    string project = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PROJECT") ?? "Proto"; // Default project
    string? pat = Environment.GetEnvironmentVariable("PAT");
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.WriteLine("PAT environment variable is not set.");
        return;
    }

    // Output the organization and project for debugging
    Console.WriteLine($"Organization: {organization}");
    Console.WriteLine($"Project: {project}");
    var helper = new AzureDevOpsWorkItemHelper(organization, project);

    // If the child task option is set, just do that and exit
    if (childTaskOfPbiId.HasValue)
    {
        await helper.CreateChildTaskWithSameTitleAsync(childTaskOfPbiId.Value);
        return;
    }

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
