using System.Xml.Linq;

namespace GitHubRelease
{
    static class Program
    {
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command-line arguments.
        /// first argument is the repo name
        /// second argument is the tag name
        /// third argument is the branch name
        /// </param>
        static async Task Main(string[] args)
        {
            Console.WriteLine($"GitHub Release demo!  args.Length: {args.Length}");

            if (args.Length < 10)
            {
                DisplayHelp();
                Environment.Exit(1);
            }

            // Set default values
            string command = string.Empty;
            string repo = string.Empty;
            string tag = string.Empty;
            string branch = string.Empty;
            string assetPath = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--command":
                    case "-c":
                        if (i + 1 < args.Length)
                        {
                            command = args[i + 1];
                            i++;
                        }
                        break;

                    case "--branch":
                    case "-b":
                        if (i + 1 < args.Length)
                        {
                            branch = args[i + 1];
                            i++;
                        }
                        break;

                    case "--path":
                    case "-p":
                        if (i + 1 < args.Length)
                        {
                            assetPath = args[i + 1];
                            i++;
                        }
                        break;

                    case "--repo":
                    case "-r":
                        if (i + 1 < args.Length)
                        {
                            repo = args[i + 1];
                            i++;
                        }
                        break;
                    case "--tag":
                    case "-t":
                        if (i + 1 < args.Length)
                        {
                            tag = args[i + 1];
                            i++;
                        }
                        break;
                }
            }

            // Check if the required arguments are provided
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(branch) || string.IsNullOrEmpty(assetPath))
            {
                Console.WriteLine("Please provide valid repo, tag, branch and asset path.");
                Environment.Exit(1);
            }
            var owner = Environment.GetEnvironmentVariable("OWNER");
            
            if (string.IsNullOrEmpty(owner))
            {
                // read test token from file
                using var reader = new StreamReader($"{Environment.GetEnvironmentVariable("USERPROFILE")}\\.owner");
                owner = reader.ReadToEnd();
            }

            var token = Environment.GetEnvironmentVariable("API_GITHUB_KEY");

            if (string.IsNullOrEmpty(token))
            {
                // read test token from file
                using var reader = new StreamReader($"{Environment.GetEnvironmentVariable("USERPROFILE")}\\.git-credentials");
                token = reader.ReadToEnd();
            }

            // Print the command line values and and exit
            Console.WriteLine($"Repo: {repo}");
            Console.WriteLine($"Tag: {tag}");
            Console.WriteLine($"Branch: {branch}");
            Console.WriteLine($"Asset Path: {assetPath}");
            
            try
            {
                switch (command)
                {
                    // get release notes
                    case "notes":
                        await GetReleaseRelease(owner, repo, tag, branch, assetPath, token);
                        break;

                    // create a release
                    case "create":
                        await CreateRelease(owner, repo, tag, branch, assetPath, token);
                        break;

                    // upload an asset
                    case "upload":
                        await UploadAsset(owner, repo, tag, branch, assetPath, token);
                        break;

                    // update a release
                    case "update":
                        await UpdateRelease(owner, repo, tag, branch, assetPath, token);
                        break;
                    default:
                        Console.WriteLine($"Invalid command '{command}'. Please use ");
                        Console.WriteLine("     'notes' get release notes since tag" );
                        Console.WriteLine("     'upload' upload an asset" );
                        Console.WriteLine("     'create' create a release");
                        Console.WriteLine("     'update' update a release");

                        Environment.Exit(1);
                        break;
                }
            }
            catch (Exception ex)
            {
                // log the type  of exception Is it a NullReferenceException, ArgumentException, etc.
                Console.WriteLine(ex.ToString());

                // log exception
                Console.WriteLine($"Exception {ex.Message}");
                Environment.Exit(1);
            }
            
            Environment.Exit(0);
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Usage: GitHubRelease --command <command> --repo <repoName> --tag <repTag> --branch <repoBranch> --path <assetPath>");
        }

        private static async Task CreateRelease(string owner, string repo, string tag, string branch, string assetPath, string token)
        {
            var releaseService = new ReleaseService(owner, repo);

            var release = new Release
            {
                TagName = tag,
                TargetCommitish = branch,  // This usually is the branch name
                Name = tag,
                Body = "Description of the release",  // should be pulled from GetLatestReleaseAsync
                Draft = false,
                Prerelease = false
            };

            // Create a release
            await releaseService.CreateRelease (token, release, assetPath);
            // In debug mode, the release will not be created
            //var responseMessage = await releaseService.CreateRelease(token, release, assetPath);
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK); // for testing debugging
            if (responseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine(responseMessage);
            }
            else
            {
                // read content
                Console.WriteLine($"Failed to create release: {responseMessage.StatusCode}");
                var content = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Environment.Exit(1);
            }
        }

        private static async Task UploadAsset(string owner, string repo, string tag, string branch, string assetPath, string token)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        private static async Task GetReleaseRelease(string owner, string repo, string tag, string branch, string assetPath, string token)
        {
            var releaseService = new ReleaseService(owner, repo);

            var release = new Release
            {
                TagName = tag,
                TargetCommitish = branch,  // This usually is the branch name
                Name = tag,
                Body = "Description of the release",  // should be pulled from GetLatestReleaseAsync
                Draft = false,
                Prerelease = false
            };

            // Create a release
            await releaseService.UpdateReleaseNotes(token, release);
            // In debug mode, the release will not be created
            //var responseMessage = await releaseService.CreateRelease(token, release, assetPath);
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK); // for testing debugging
            if (responseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine(responseMessage);

                // output the release notes
                Console.WriteLine("Release Notes:");
                Console.WriteLine($"{release.Body}");
            }
            else
            {
                // read content
                Console.WriteLine($"Failed to create release: {responseMessage.StatusCode}");
                var content = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Environment.Exit(1);
            }

        }

        private static async Task UpdateRelease(string owner, string repo, string tag, string branch, string assetPath, string token)
        {
            var releaseService = new ReleaseService(owner, repo);

            var release = new Release
            {
                TagName = tag,
                TargetCommitish = branch,  // This usually is the branch name
                Name = tag,
                Body = "Description of the release",  // should be pulled from GetLatestReleaseAsync
                Draft = false,
                Prerelease = false
            };

            // Create a release
            await releaseService.UpdateReleaseNotes(token, release);
            // In debug mode, the release will not be created
            //var responseMessage = await releaseService.CreateRelease(token, release, assetPath);
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK); // for testing debugging
            if (responseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine(responseMessage);
            }
            else
            {
                // read content
                Console.WriteLine($"Failed to create release: {responseMessage.StatusCode}");
                var content = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Environment.Exit(1);
            }
        }
    }
}
