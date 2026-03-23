using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Sdo.Commands;
using Sdo.Mapping;
using Sdo.Services;

namespace SdoTests
{
    class TestMappingPresenter : IMappingPresenter
    {
        public string? Last { get; private set; }
        public void Present(string mapping)
        {
            Last = mapping;
        }
    }

    public class PullRequestCommandMappingTests
    {
        private readonly Option<bool> _verboseOption = new Option<bool>("--verbose");

        [Fact]
        public async Task CreateCommand_DryRun_PresentsMapping()
        {
            var presenter = new TestMappingPresenter();
            var generator = new Sdo.Mapping.MappingGenerator();
            var cmd = new PullRequestCommand(_verboseOption, generator, presenter);

            // Prepare temp markdown file
            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, "# Test PR\n\nBody");

            // Force platform detector internals to GitHub with owner/repo
            var platField = typeof(PullRequestCommand).GetField("_platformDetector", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(platField);
            var platInstance = platField.GetValue(cmd);
            Assert.NotNull(platInstance);
            // Set private fields on PlatformService
            var detField = platInstance.GetType().GetField("_detectedPlatform", BindingFlags.NonPublic | BindingFlags.Instance);
            var orgField = platInstance.GetType().GetField("_organization", BindingFlags.NonPublic | BindingFlags.Instance);
            var projField = platInstance.GetType().GetField("_project", BindingFlags.NonPublic | BindingFlags.Instance);
            detField.SetValue(platInstance, Sdo.Interfaces.Platform.GitHub);
            orgField.SetValue(platInstance, "ownerX");
            projField.SetValue(platInstance, "repoY");

            // Invoke private CreatePullRequest with dryRun=true and verbose=true
            var method = typeof(PullRequestCommand).GetMethod("CreatePullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task<int>)method.Invoke(cmd, new object[] { tmp, 0, false, true, true })!;
            var result = await task;
            Assert.Equal(0, result);

            // Determine branch used by command (GetCurrentBranch may return the repo's current branch)
            var getBranchMethod = typeof(PullRequestCommand).GetMethod("GetCurrentBranch", BindingFlags.NonPublic | BindingFlags.Instance);
            var branch = (string?)getBranchMethod.Invoke(cmd, Array.Empty<object>());
            var expected = generator.PrCreateGitHub("ownerX", "repoY", "Test PR", tmp, branch ?? "main", "main", false);
            Assert.Equal(expected, presenter.Last);

            File.Delete(tmp);
        }

        [Fact]
        public async Task ListCommand_Verbose_PresentsMapping()
        {
            // Ensure env token exists so GetGitHubTokenAsync returns a non-null value
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", "fake-token-for-tests");

            var presenter = new TestMappingPresenter();
            var generator = new Sdo.Mapping.MappingGenerator();
            var cmd = new PullRequestCommand(_verboseOption, generator, presenter);

            // Configure platform detector internals
            var platField = typeof(PullRequestCommand).GetField("_platformDetector", BindingFlags.NonPublic | BindingFlags.Instance);
            var platInstance = platField.GetValue(cmd);
            var detField = platInstance.GetType().GetField("_detectedPlatform", BindingFlags.NonPublic | BindingFlags.Instance);
            var orgField = platInstance.GetType().GetField("_organization", BindingFlags.NonPublic | BindingFlags.Instance);
            var projField = platInstance.GetType().GetField("_project", BindingFlags.NonPublic | BindingFlags.Instance);
            detField.SetValue(platInstance, Sdo.Interfaces.Platform.GitHub);
            orgField.SetValue(platInstance, "ownerA");
            projField.SetValue(platInstance, "repoB");

            var method = typeof(PullRequestCommand).GetMethod("ListPullRequests", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<int>)method.Invoke(cmd, new object[] { "open", 5, true })!;
            var result = await task;

            // mapping should have been presented
            var expected = generator.PrListGitHub("ownerA", "repoB", "open", 5);
            Assert.Equal(expected, presenter.Last);
        }

        [Fact]
        public async Task ShowCommand_Verbose_PresentsMapping()
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", "fake-token-for-tests");
            var presenter = new TestMappingPresenter();
            var generator = new Sdo.Mapping.MappingGenerator();
            var cmd = new PullRequestCommand(_verboseOption, generator, presenter);

            var platField = typeof(PullRequestCommand).GetField("_platformDetector", BindingFlags.NonPublic | BindingFlags.Instance);
            var platInstance = platField.GetValue(cmd);
            var detField = platInstance.GetType().GetField("_detectedPlatform", BindingFlags.NonPublic | BindingFlags.Instance);
            var orgField = platInstance.GetType().GetField("_organization", BindingFlags.NonPublic | BindingFlags.Instance);
            var projField = platInstance.GetType().GetField("_project", BindingFlags.NonPublic | BindingFlags.Instance);
            detField.SetValue(platInstance, Sdo.Interfaces.Platform.GitHub);
            orgField.SetValue(platInstance, "ownerA");
            projField.SetValue(platInstance, "repoB");

            var method = typeof(PullRequestCommand).GetMethod("ShowPullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<int>)method.Invoke(cmd, new object[] { 123, true })!;
            var result = await task;

            var expected = $"gh pr view -R ownerA/repoB 123";
            Assert.Equal(expected, presenter.Last);
        }

        [Fact]
        public async Task UpdateCommand_Verbose_PresentsMapping()
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", "fake-token-for-tests");
            var presenter = new TestMappingPresenter();
            var generator = new Sdo.Mapping.MappingGenerator();
            var cmd = new PullRequestCommand(_verboseOption, generator, presenter);

            var platField = typeof(PullRequestCommand).GetField("_platformDetector", BindingFlags.NonPublic | BindingFlags.Instance);
            var platInstance = platField.GetValue(cmd);
            var detField = platInstance.GetType().GetField("_detectedPlatform", BindingFlags.NonPublic | BindingFlags.Instance);
            var orgField = platInstance.GetType().GetField("_organization", BindingFlags.NonPublic | BindingFlags.Instance);
            var projField = platInstance.GetType().GetField("_project", BindingFlags.NonPublic | BindingFlags.Instance);
            detField.SetValue(platInstance, Sdo.Interfaces.Platform.GitHub);
            orgField.SetValue(platInstance, "ownerA");
            projField.SetValue(platInstance, "repoB");

            var method = typeof(PullRequestCommand).GetMethod("UpdatePullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<int>)method.Invoke(cmd, new object[] { 321, "New Title", "closed", true })!;
            var result = await task;

            var expectedStart = $"gh pr edit -R ownerA/repoB 321";
            Assert.NotNull(presenter.Last);
            Assert.StartsWith(expectedStart, presenter.Last!);
            Assert.Contains("--title \"New Title\"", presenter.Last);
            Assert.Contains("--state closed", presenter.Last);
        }
    }
}
