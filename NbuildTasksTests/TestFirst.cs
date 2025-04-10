﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
            GitHubActions = githubActionsEnvironment != null;
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
            // extract project name from url
            ProjectName = TestProject.Split('/').Last().Split('.').First();
            Assert.IsNotNull(ProjectName);

            var gitWrapper = new GitWrapper(ProjectName, verbose:true, testMode:true);

            try
            {
                var solutionDir = $@"{gitWrapper.DevDrive}\{gitWrapper.MainDir}\{ProjectName}";
                if (!Directory.Exists(solutionDir))
                {
                    Assert.AreEqual(0, gitWrapper.CloneProject(TestProject).Code);
                }
                Assert.IsTrue(Directory.Exists(solutionDir));

                // change to project directory
                Directory.SetCurrentDirectory(solutionDir);

                Assert.IsTrue(File.Exists($"README.md"));

                // create 'testRandom' branch if not exists
                if (!gitWrapper.BranchExists(TestBranch))
                {
                    Assert.IsTrue(gitWrapper.CheckoutBranch(TestBranch, create: true));
                }
                Assert.IsTrue(gitWrapper.CheckoutBranch(TestBranch));

                if (string.IsNullOrEmpty(gitWrapper.Tag))
                {
                    Assert.IsTrue(gitWrapper.SetTag("1.0.0"));
                }

                Assert.IsNotNull(gitWrapper.Tag);

                Console.WriteLine($"TestFirst - AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine($"TestFirst - DevDrive: {gitWrapper.DevDrive}");
                Console.WriteLine($"TestFirst - MainDir: {gitWrapper.MainDir}");
                Console.WriteLine($"TestFirst - Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"TestFirst - Current Branch: {gitWrapper.Branch}");
                Console.WriteLine($"TestFirst - Current Tag: {gitWrapper.Tag}");
            }
            catch (Exception ex)
            {
                

                Assert.Fail($"TestFirst exception {ex.Message}");
            }
            finally
            {
                // change back to test directory
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
        }
    }
}
