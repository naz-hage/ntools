using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nbuild.Helpers;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// User management commands (GitHub & Azure DevOps).
    /// Follows the same pattern as other commands (detect platform, get token, call client).
    /// Reference: Python implementation available under c:\source\saz for behavior details.
    /// </summary>
    public class UserCommand : Command
    {
        private readonly PlatformService _platformDetector;

        public UserCommand(Option<bool> verboseOption) : base("user", "User management commands for GitHub and Azure DevOps")
        {
            _platformDetector = new PlatformService();

            AddShowCommand(verboseOption);
            AddListCommand(verboseOption);
            AddSearchCommand(verboseOption);
            AddPermissionsCommand(verboseOption);
        }

        private void AddShowCommand(Option<bool> verboseOption)
        {
            var cmd = new Command("show", "Show user details (by login or id)");
            var loginOpt = new Option<string?>("--login") { Description = "User login or id" };
            cmd.Add(loginOpt);
            cmd.Add(verboseOption);
            cmd.SetAction(async (pr) =>
            {
                var login = pr.GetValue(loginOpt);
                var verbose = pr.GetValue(verboseOption);
                return await ShowUser(login, verbose);
            });
            Subcommands.Add(cmd);
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var cmd = new Command("list", "List users/collaborators in the current repository or organization");
            var topOpt = new Option<int>("--top") { Description = "Limit results" };
            cmd.Add(topOpt);
            cmd.Add(verboseOption);
            cmd.SetAction(async (pr) =>
            {
                var top = pr.GetValue(topOpt);
                var verbose = pr.GetValue(verboseOption);
                return await ListUsers(top, verbose);
            });
            Subcommands.Add(cmd);
        }

        private void AddSearchCommand(Option<bool> verboseOption)
        {
            var cmd = new Command("search", "Search users by name or email (GitHub only)");
            var qOpt = new Option<string?>("--query") { Description = "Search query (name, email, login)" };
            cmd.Add(qOpt);
            cmd.Add(verboseOption);
            cmd.SetAction(async (pr) =>
            {
                var q = pr.GetValue(qOpt);
                var verbose = pr.GetValue(verboseOption);
                return await SearchUsers(q, verbose);
            });
            Subcommands.Add(cmd);
        }

        private void AddPermissionsCommand(Option<bool> verboseOption)
        {
            var cmd = new Command("permissions", "Show permissions for a user or current identity (org/project)");
            var userOpt = new Option<string?>("--user") { Description = "User login or id" };
            cmd.Add(userOpt);
            cmd.Add(verboseOption);
            cmd.SetAction(async (pr) =>
            {
                var user = pr.GetValue(userOpt);
                var verbose = pr.GetValue(verboseOption);
                return await ShowPermissions(user, verbose);
            });
            Subcommands.Add(cmd);
        }

        private async Task<int> ShowUser(string? login, bool verbose)
        {
            try
            {
                var platform = _platformDetector.DetectPlatform();
                if (verbose) ConsoleHelper.WriteLine($"Detected platform: {platform}", ConsoleColor.Green);

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        var mappingGen = new Sdo.Mapping.MappingGenerator();
                            var repo = _platformDetector.GetRepositoryInfo();
                            if (repo != null && !string.IsNullOrEmpty(repo.Owner) && !string.IsNullOrEmpty(repo.Repo))
                            {
                                var mappingCmd = mappingGen.CollaboratorsListGitHub(repo.Owner, repo.Repo, 100);
                                if (!string.IsNullOrEmpty(mappingCmd)) ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                            }
                    }
                    var auth = new AuthenticationService();
                    var token = await auth.GetGitHubTokenAsync();
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X No GitHub token found", ConsoleColor.Red);
                        return 1;
                    }
                    var client = new GitHubClient(token);
                    if (string.IsNullOrEmpty(login))
                    {
                        var user = await client.GetUserAsync();
                        if (user == null) { ConsoleHelper.WriteLine("X User not found", ConsoleColor.Red); return 1; }
                        Console.WriteLine($"User: {user.Login} — {user.Name}");
                        return 0;
                    }
                    var found = await client.GetUserAsync(login);
                    if (found == null) { ConsoleHelper.WriteLine("X User not found", ConsoleColor.Red); return 1; }
                    Console.WriteLine($"User: {found.Login} — {found.Name}");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        var mappingGen = new Sdo.Mapping.MappingGenerator();
                        var mappingOrg = _platformDetector.GetOrganization() ?? "(organization)";
                        var mappingProject = _platformDetector.GetProject() ?? "(project)";
                        var mappingCmd = mappingGen.ListUsersAzure(mappingOrg, mappingProject, 1000);
                        if (!string.IsNullOrEmpty(mappingCmd)) ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                    }
                    var auth = new AuthenticationService();
                    var token = await auth.GetAzureDevOpsTokenAsync();
                    if (string.IsNullOrEmpty(token)) { ConsoleHelper.WriteLine("X No Azure DevOps PAT found", ConsoleColor.Red); return 1; }
                    var org = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(org)) { ConsoleHelper.WriteLine("X Could not determine organization", ConsoleColor.Red); return 1; }
                    var client = new AzureDevOpsClient(token, org);
                    // Azure DevOps user lookup placeholder — implementation depends on Graph APIs
                    var user = await client.GetUserAsync(login);
                    if (user == null) { ConsoleHelper.WriteLine("X User not found", ConsoleColor.Red); return 1; }
                    Console.WriteLine($"User: {user.DisplayName} — {user.UniqueName}");
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        private async Task<int> ListUsers(int top, bool verbose)
        {
            try
            {
                var platform = _platformDetector.DetectPlatform();
                // When verbose, show equivalent native CLI mapping (gh / az)
                if (verbose)
                {
                    var mappingGen = new Sdo.Mapping.MappingGenerator();
                    if (platform == Platform.GitHub)
                    {
                        var repo = _platformDetector.GetRepositoryInfo();
                        if (repo != null && !string.IsNullOrEmpty(repo.Owner) && !string.IsNullOrEmpty(repo.Repo))
                        {
                            var mappingCmd = mappingGen.CollaboratorsListGitHub(repo.Owner, repo.Repo, top > 0 ? top : 100);
                            if (!string.IsNullOrEmpty(mappingCmd)) ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                        }
                    }
                    else if (platform == Platform.AzureDevOps)
                    {
                        var mappingOrg = _platformDetector.GetOrganization() ?? "(organization)";
                        var mappingProject = _platformDetector.GetProject() ?? "(project)";
                        var mappingCmd = mappingGen.ListUsersAzure(mappingOrg, mappingProject, top > 0 ? top : 1000);
                        if (!string.IsNullOrEmpty(mappingCmd)) ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                    }
                }
                if (platform == Platform.GitHub)
                {
                    var auth = new AuthenticationService();
                    var token = await auth.GetGitHubTokenAsync();
                    if (string.IsNullOrEmpty(token)) { ConsoleHelper.WriteLine("X No GitHub token found", ConsoleColor.Red); return 1; }
                    var client = new GitHubClient(token);
                    var repo = _platformDetector.GetRepositoryInfo();
                    if (repo == null) { ConsoleHelper.WriteLine("X Could not determine repository", ConsoleColor.Red); return 1; }

                    if (verbose)
                    {
                        // show mapping
                    }

                    var collabs = await client.ListCollaboratorsAsync(repo.Owner!, repo.Repo!, top > 0 ? top : 100);
                    if (collabs == null || collabs.Count == 0) { Console.WriteLine("No users found"); return 0; }

                    // Header similar to saz output
                    ConsoleHelper.WriteLine($"Users in repository '{repo.Owner}/{repo.Repo}' ({collabs.Count} total):", ConsoleColor.Green);
                    Console.WriteLine(new string('-', 80));
                    foreach (var c in collabs)
                    {
                        var name = string.IsNullOrEmpty(c.Name) ? "(no display name)" : c.Name;
                        Console.WriteLine($"  {name} — {c.Login}");
                    }
                    Console.WriteLine();
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var auth = new AuthenticationService();
                    var token = await auth.GetAzureDevOpsTokenAsync();
                    if (string.IsNullOrEmpty(token)) { ConsoleHelper.WriteLine("X No Azure DevOps PAT found", ConsoleColor.Red); return 1; }
                    var org = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(org)) { ConsoleHelper.WriteLine("X Could not determine organization", ConsoleColor.Red); return 1; }
                    var client = new AzureDevOpsClient(token, org);
                    var project = _platformDetector.GetProject();


                    if (verbose)
                    {
                        // show mapping
                    }

                    var users = await client.ListUsersAsync(top > 0 ? top : 1000);
                    if (users == null || users.Count == 0) { Console.WriteLine("No users found"); return 0; }

                    // Print extracted info and a friendly header like the Python saz output
                    ConsoleHelper.WriteLine("✓ Extracted Azure DevOps information from Git remote:", ConsoleColor.Green);
                    Console.WriteLine($"  Organization: {org}");
                    Console.WriteLine($"  Project: {project}");
                    Console.WriteLine();

                    Console.WriteLine($"Users in organization '{org}' ({users.Count} total):");
                    Console.WriteLine(new string('-', 80));
                    foreach (var u in users)
                    {
                        Console.WriteLine("---");
                        if (!string.IsNullOrEmpty(u.DisplayName)) Console.WriteLine($"DisplayName: {u.DisplayName}");
                        if (!string.IsNullOrEmpty(u.UniqueName)) Console.WriteLine($"UniqueName: {u.UniqueName}");

                        // Try to extract a GUID-like id from descriptor or show a shortened descriptor
                        if (!string.IsNullOrEmpty(u.Id))
                        {
                            var id = u.Id;
                            // regex for GUID
                            var guidMatch = System.Text.RegularExpressions.Regex.Match(id, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                            if (guidMatch.Success)
                            {
                                Console.WriteLine($"Id: {guidMatch.Value}");
                            }
                            else
                            {
                                // Truncate long service descriptors to keep output readable
                                if (id.Length > 48)
                                {
                                    var shortId = id.Substring(0, 24) + "..." + id.Substring(id.Length - 12);
                                    Console.WriteLine($"Descriptor: {shortId}");
                                }
                                else
                                {
                                    Console.WriteLine($"Descriptor: {id}");
                                }
                            }
                        }

                        Console.WriteLine();
                    }
                    Console.WriteLine(new string('-', 80));
                    return 0;
                }
                ConsoleHelper.WriteLine("X Unsupported platform", ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        private async Task<int> SearchUsers(string? query, bool verbose)
        {
            if (string.IsNullOrEmpty(query))
            {
                ConsoleHelper.WriteLine("X Query is required for search", ConsoleColor.Red);
                return 1;
            }
            try
            {
                var platform = _platformDetector.DetectPlatform();
                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        var mappingGen = new Sdo.Mapping.MappingGenerator();
                        var mappingCmd = mappingGen.SearchUsersGitHub(query, 100);
                        if (!string.IsNullOrEmpty(mappingCmd)) ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                    }
                    var auth = new AuthenticationService();
                    var token = await auth.GetGitHubTokenAsync();
                    if (string.IsNullOrEmpty(token)) { ConsoleHelper.WriteLine("X No GitHub token found", ConsoleColor.Red); return 1; }
                    var client = new GitHubClient(token);
                    var results = await client.SearchUsersAsync(query);
                    if (results == null || results.Count == 0) { Console.WriteLine("No users found"); return 0; }
                    foreach (var r in results) Console.WriteLine($"{r.Login} — {r.Name}");
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteLine("X Search is only supported for GitHub in this version", ConsoleColor.Yellow);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        private async Task<int> ShowPermissions(string? user, bool verbose)
        {
            try
            {
                var platform = _platformDetector.DetectPlatform();
                // When verbose, show equivalent native CLI mapping (gh / az)
                if (verbose)
                {
                    var mappingGen = new Sdo.Mapping.MappingGenerator();
                    string mappingCmd = string.Empty;
                    if (platform == Platform.GitHub)
                    {
                        var repo = _platformDetector.GetRepositoryInfo();
                        if (repo != null && !string.IsNullOrEmpty(repo.Owner) && !string.IsNullOrEmpty(repo.Repo))
                        {
                            mappingCmd = mappingGen.RepoPermissionGitHub(repo.Owner, repo.Repo, user ?? "<user>");
                        }
                    }
                    else if (platform == Platform.AzureDevOps)
                    {
                        var org = _platformDetector.GetOrganization() ?? "(organization)";
                        var project = _platformDetector.GetProject() ?? "(project)";
                        mappingCmd = mappingGen.UserPermissionsAzure(org, project, user ?? "<descriptor>");
                    }

                    if (!string.IsNullOrEmpty(mappingCmd))
                    {
                        ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                    }
                }
                if (platform == Platform.GitHub)
                {
                    var auth = new AuthenticationService();
                    var token = await auth.GetGitHubTokenAsync();
                    if (string.IsNullOrEmpty(token)) { ConsoleHelper.WriteLine("X No GitHub token found", ConsoleColor.Red); return 1; }
                    var client = new GitHubClient(token);
                    var repo = _platformDetector.GetRepositoryInfo();
                    if (repo == null) { ConsoleHelper.WriteLine("X Could not determine repository", ConsoleColor.Red); return 1; }
                    var perm = await client.GetRepositoryPermissionAsync(repo.Owner!, repo.Repo!, user, verbose);
                    Console.WriteLine(perm ?? "No permission info available");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var auth = new AuthenticationService();
                    var token = await auth.GetAzureDevOpsTokenAsync();
                    if (string.IsNullOrEmpty(token)) { ConsoleHelper.WriteLine("X No Azure DevOps PAT found", ConsoleColor.Red); return 1; }
                    var org = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(org)) { ConsoleHelper.WriteLine("X Could not determine organization", ConsoleColor.Red); return 1; }
                    var client = new AzureDevOpsClient(token, org);
                    var perms = await client.GetUserPermissionsAsync(user, verbose);
                    if (perms == null) { Console.WriteLine("No permission info available"); return 0; }
                    foreach (var p in perms) Console.WriteLine($"{p.Key}: {p.Value}");
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }
    }
}
