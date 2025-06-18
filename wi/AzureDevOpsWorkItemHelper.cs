using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AzureDevOpsWorkItemHelper
{
    private readonly string _organization;
    private readonly string _project;
    private readonly string _pat;

    public AzureDevOpsWorkItemHelper(string organization, string project)
    {
        _organization = organization;
        _project = project;
        _pat = Environment.GetEnvironmentVariable("PAT")!; // Ensure PAT is set in the environment variables
        if (string.IsNullOrWhiteSpace(_pat))
        {
            Console.WriteLine("PAT environment variable is not set.");
            throw new InvalidOperationException("Personal Access Token (PAT) is required.");
        }
    }

    public async Task CreateChildTaskWithSameTitleAsync(int pbiId)
    {
        using var client = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_pat}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        //1. Get the title of the PBI
        var title = await GetWorkItemTitleAsync(client, pbiId);
        if (title == null)
            return;

        // 2. Create the Task with the same title, linked to the PBI
        var taskUri = $"{_organization}/{_project}/_apis/wit/workitems/$Task?api-version={AzureDevOpsApiVersions.WorkItems}";
        var body = new object[]
        {
            new {
                op = "add",
                path = "/fields/System.Title",
                value = title
            },
            new {
                op = "add",
                path = "/relations/-",
                value = new {
                    rel = "System.LinkTypes.Hierarchy-Reverse",
                    url = $"{_organization}/_apis/wit/workItems/{pbiId}",
                    attributes = new {
                        comment = "Linking to parent PBI"
                    }
                }
            }
        };
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

        var postResponse = await client.PatchAsync(taskUri, content);
        if (postResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Created Task '{title}' under PBI {pbiId}");
        }
        else
        {
            var respContent = await postResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Failed to create Task for PBI {pbiId}. Status: {postResponse.StatusCode}. Response: {respContent}");
        }
    }

    private async Task<string?> GetWorkItemTitleAsync(HttpClient client, int workItemId)
    {
        var getUri = $"{_organization}/{_project}/_apis/wit/workitems/{workItemId}?api-version={AzureDevOpsApiVersions.Pipelines}";
        var getResponse = await client.GetAsync(getUri);
        if (!getResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to get work item {workItemId}. Status: {getResponse.StatusCode}");
            return null;
        }

        using var getStream = await getResponse.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(getStream);
        if (!doc.RootElement.TryGetProperty("fields", out var fields) ||
            !fields.TryGetProperty("System.Title", out var titleElement))
        {
            Console.WriteLine($"Could not retrieve title for work item {workItemId}");
            return null;
        }
        return titleElement.GetString();
    }

    public async Task<int?> CreatePbiAsync(string title, int parentId)
    {
        using var client = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_pat}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var uri = $"{_organization}/{_project}/_apis/wit/workitems/$Product%20Backlog%20Item?api-version={AzureDevOpsApiVersions.WorkItems}";

        var body = new object[]
        {
            new
            {
                op = "add",
                path = "/fields/System.Title",
                value = title
            },
            new
            {
                op = "add",
                path = "/relations/-",
                value = new
                {
                    rel = "System.LinkTypes.Hierarchy-Reverse",
                    url = $"{_organization}/_apis/wit/workItems/{parentId}",
                    attributes = new
                    {
                        comment = "Linking to parent work item"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

        try
        {
            var response = await client.PatchAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                if (doc.RootElement.TryGetProperty("id", out var idElement))
                {
                    int id = idElement.GetInt32();
                    Console.WriteLine($"Created PBI '{title}' with parent ID {parentId}, new PBI ID: {id}");
                    return id;
                }
                else
                {
                    Console.WriteLine($"Created PBI '{title}' with parent ID {parentId}, but could not parse ID.");
                    return null;
                }
            }
            else
            {
                var respContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to create PBI '{title}'. Status: {response.StatusCode}. Response: {respContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create PBI '{title}'. Error: {ex.Message}");
            return null;
        }
    }

}

// Extension method for PATCH
public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
    {
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
        return client.SendAsync(request);
    }
}
