﻿using NbuildTasks;
using System.Xml.Linq;

namespace GitHubRelease
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"{Nversion.Get()}\n");

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
                        await GetReleaseRelease(repo, tag, branch, assetPath);
                        break;

                    // create a release
                    case "create":
                        await CreateRelease(repo, tag, branch, assetPath);
                        break;

                    // upload an asset
                    case "upload":
                        await UploadAsset(repo, tag, branch, assetPath);
                        break;

                    // update a release
                    case "update":
                        await UpdateRelease(repo, tag, branch, assetPath);
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

        private static async Task CreateRelease(string repo, string tag, string branch, string assetPath)
        {

            var releaseService = new ReleaseService(repo);

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
            await releaseService.CreateRelease (release, assetPath);
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

        private static async Task UploadAsset(string repo, string tag, string branch, string assetPath)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        private static async Task GetReleaseRelease(string repo, string tag, string branch, string assetPath)
        {
            var owner = Credentials.GetOwner();
            var releaseService = new ReleaseService(repo);

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
            var token = Credentials.GetToken();
            await releaseService.UpdateReleaseNotes(release);
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

        private static async Task UpdateRelease(string repo, string tag, string branch, string assetPath)
        {
            var releaseService = new ReleaseService(repo);

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
            await releaseService.UpdateReleaseNotes(release);
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
