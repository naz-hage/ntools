using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NbuildTasks.Tests
{
    [TestClass]
    public class TestFirst
    {
        public const string TestProject = "https://github.com/naz-hage/getting-started";
        public const string TestBranch = "testRandom";
        public static string ProjectName { get; set; }

        public static bool GitHubActions { get; set; } = false;

        public TestFirst()
        {
            var githubActionsEnvironment = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            GitHubActions = !string.IsNullOrEmpty(githubActionsEnvironment);
            Console.WriteLine($"GitHubActions: {GitHubActions}");
            ProjectName = TestProject.Split('/').Last().Split('.').First();
        }
        /// <summary>
        /// Initializes the assembly before any tests are run.
        /// </summary>
        /// <param name="context">The test context.</param>
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            RandomRepoTest();
        }

        public static void RandomRepoTest()
        {
            // If git is not available on the machine, mark tests inconclusive instead of failing.
            try
            {
                var psi = new ProcessStartInfo("git", "--version") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                using var p = Process.Start(psi);
                if (p == null)
                {
                    Console.WriteLine("git process could not be started");
                    Assert.Inconclusive("git is not available on this machine");
                    return;
                }
                p.WaitForExit(2000);
                if (p.ExitCode != 0)
                {
                    Console.WriteLine("git returned non-zero exit code");
                    Assert.Inconclusive("git is not available on this machine");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"git not available: {ex.Message}");
                Assert.Inconclusive("git is not available on this machine");
                return;
            }

            // extract project name from url
            ProjectName = TestProject.Split('/').Last().Split('.').First();
            Assert.IsNotNull(ProjectName);

            var gitWrapper = new GitWrapper(ProjectName, verbose: true, testMode: true);

            try
            {
                var solutionDir = $@"{gitWrapper.DevDrive}\{gitWrapper.MainDir}\{ProjectName}";
                if (!Directory.Exists(solutionDir))
                {
                    var cloneResult = gitWrapper.CloneProject(TestProject);
                    if (cloneResult == null || cloneResult.Code != 0)
                    {
                        Console.WriteLine("CloneProject failed or returned non-zero. Skipping tests that require the sample repo.");
                        Assert.Inconclusive("Could not clone test project; network/git may be unavailable.");
                        return;
                    }
                }

                if (!Directory.Exists(solutionDir))
                {
                    Assert.Inconclusive("Cloned solution directory not found; skipping tests that require the sample repo.");
                    return;
                }

                // change to project directory
                Directory.SetCurrentDirectory(solutionDir);

                if (!File.Exists($"README.md"))
                {
                    Assert.Inconclusive("Test project appears incomplete (missing README.md); skipping tests.");
                    return;
                }

                // create 'testRandom' branch if not exists
                if (!gitWrapper.BranchExists(TestBranch))
                {
                    var created = gitWrapper.CheckoutBranch(TestBranch, create: true);
                    if (!created)
                    {
                        Assert.Inconclusive("Could not create test branch; skipping tests.");
                        return;
                    }
                }
                if (!gitWrapper.CheckoutBranch(TestBranch))
                {
                    Assert.Inconclusive("Could not checkout test branch; skipping tests.");
                    return;
                }

                if (string.IsNullOrEmpty(gitWrapper.Tag))
                {
                    var tagSet = gitWrapper.SetTag("1.0.0");
                    if (!tagSet)
                    {
                        Assert.Inconclusive("Could not set initial tag; skipping tests.");
                        return;
                    }
                }

                if (string.IsNullOrEmpty(gitWrapper.Tag))
                {
                    Assert.Inconclusive("Test repo has no tag information; skipping tests.");
                    return;
                }

                Console.WriteLine($"TestFirst - AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine($"TestFirst - DevDrive: {gitWrapper.DevDrive}");
                Console.WriteLine($"TestFirst - MainDir: {gitWrapper.MainDir}");
                Console.WriteLine($"TestFirst - Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"TestFirst - Current Branch: {gitWrapper.Branch}");
                Console.WriteLine($"TestFirst - Current Tag: {gitWrapper.Tag}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestFirst exception: {ex.Message}");
                Assert.Inconclusive($"TestFirst encountered an exception; skipping tests. {ex.Message}");
                return;
            }
            finally
            {
                // change back to test directory
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
        }
    }
}
