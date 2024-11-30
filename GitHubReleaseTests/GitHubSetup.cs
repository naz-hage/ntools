using System.Text.Json;

namespace GitHubRelease.Tests
{
    public class GitHubSetup
    {
        protected readonly string Owner = "naz-hage";
        protected readonly string Repo = "learn"; //"ntools";//"learn";
        protected readonly string Token = System.IO.File.ReadAllText(System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.git-credentials");

        protected readonly string MainBranch = "main";
        protected readonly string DefaultBranch = "main";//"58-issue";// "13-issue";
        protected readonly string TagStagingRequested = "1.2.1";
        protected readonly string TagProductionRequested = "1.3.0";

        protected JsonSerializerOptions Options = new() { WriteIndented = true };

        public GitHubSetup()
        {
            // private properties to console
            Console.WriteLine($"Owner: {Owner}");
            Console.WriteLine($"Repo: {Repo}");
            Console.WriteLine($"DefaultBranch: {DefaultBranch}");


        }

        protected string CreateAsset(string tag)
        {
            var assetPath = $"C:\\Artifacts\\{Repo}\\release\\{tag}.zip";
            if (File.Exists(assetPath))
            {
                return assetPath;
            }
            else
            {
                File.Copy($"C:\\Artifacts\\{Repo}\\release\\0.0.1.zip", assetPath);
                return assetPath;
            }
        }
    }
}