using System.Text;
using System.Text.Json;

namespace GitHubRelease
{
    public class ContributorService(ApiService apiService, string repo) : Constants
    {
        private readonly ApiService apiService = apiService;
        private readonly string Repo = repo;

        private async Task<List<string>> GetAllContributorsAsync()
        {
            var contributors = new List<string>();

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/contributors";
            Console.WriteLine($"GET uri: {uri}");
            var response = await apiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {

                var content = await response.Content.ReadAsStringAsync();
                var allContributors = JsonDocument.Parse(content).RootElement.EnumerateArray();
                foreach (var contributor in allContributors)
                {
                    contributors.Add(contributor.GetProperty("login").GetString() ?? string.Empty);
                }

                // remove duplicates and empty strings
                contributors = contributors.Distinct().Where(c => !string.IsNullOrEmpty(c)).ToList();
            }

            return contributors;
        }

        public async Task<StringBuilder> GetNewContributorsAsync(List<JsonElement> commits)
        {
            StringBuilder releaseNotes = new();
            var contributors = await GetAllContributorsAsync();

            foreach (var commit in commits)
            {
                try
                {
                    var author = commit.GetProperty("commit").GetProperty(AuthorPropertyName).GetProperty("name").GetString();
                    if (author != null && !contributors.Contains(author))
                    {
                        var prUri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/commits/{commit.GetProperty("sha").GetString()}/pulls";
                        Console.WriteLine($"GET uri: {prUri}");
                        var prResponse = await apiService.GetAsync(prUri);
                        if (prResponse.IsSuccessStatusCode)
                        {
                            var prContent = await prResponse.Content.ReadAsStringAsync();
                            var pulls = JsonDocument.Parse(prContent).RootElement.EnumerateArray();
                            if (pulls.Any())
                            {
                                var prNumber = pulls.ElementAt(0).GetProperty("number").GetString();
                                releaseNotes.AppendLine($"* @{author} made their first contribution in https://github.com/{Credentials.GetOwner()}/{Repo}/pull/{prNumber}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // log exception
                    Console.WriteLine($"Exception ignored: {ex.Message}");
                }
            }

            return releaseNotes;
        }

    }
}