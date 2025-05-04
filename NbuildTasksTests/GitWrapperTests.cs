using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ntools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static NbuildTasks.Enums;


namespace NbuildTasks.Tests
{
    [TestClass()]
    public class GitWrapperTests : TestFirst
    {
        private readonly GitWrapper GitWrapper = new(ProjectName, verbose: true, testMode: true);

        private const string ValidUrl = "https://github.com/naz-hage/getting-started";
        private const string InvalidUrl = "invalid-url";
        private string TempSourceDir;

        public GitWrapperTests()
        {
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"GitWrapper.Parameters.WorkingDir: {GitWrapper.WorkingDirectory}");
            //Assert.AreEqual(GitWrapper.WorkingDirectory, Directory.GetCurrentDirectory());
            Console.WriteLine($"Current Branch: {GitWrapper.Branch}");
            Console.WriteLine($"Current Tag: {GitWrapper.Tag}");
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a temporary source directory for testing
            TempSourceDir = Path.Combine(Path.GetTempPath(), "GitWrapperTests");
            if (!Directory.Exists(TempSourceDir))
            {
                Directory.CreateDirectory(TempSourceDir);
            }
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void TestCleanup()
        {
            // Clean up the temporary source directory after each test
            if (Directory.Exists(TempSourceDir))
            {
                Directory.Delete(TempSourceDir, true);
            }
        }

        [TestMethod]
        public void CloneProject_ShouldSucceed_WithValidUrl()
        {
            // Arrange
            var gitWrapper = new GitWrapper(verbose: true);

            // Act
            var result = gitWrapper.CloneProject(ValidUrl, TempSourceDir);

            // Assert
            Assert.IsTrue(result.IsSuccess(), "CloneProject should succeed for a valid URL.");
            var projectName = GitWrapper.ProjectNameFromUrl(ValidUrl);
            var projectPath = Path.Combine(TempSourceDir, projectName);
            Assert.IsTrue(Directory.Exists(projectPath), "The project directory should be created.");

            CloneProject_ShouldFail_WhenProjectAlreadyExists();
        }

        [TestMethod]
        public void CloneProject_ShouldFail_WithInvalidUrl()
        {
            // Arrange
            var gitWrapper = new GitWrapper(verbose: true);

            // Act
            var result = gitWrapper.CloneProject(InvalidUrl, TempSourceDir);

            // Assert
            Assert.IsFalse(result.IsSuccess(), "CloneProject should fail for an invalid URL.");
            Assert.AreEqual(ResultHelper.InvalidParameter, result.Code, "Expected invalid parameter error code.");
        }

        public void CloneProject_ShouldFail_WhenProjectAlreadyExists()
        {
            // Arrange
            var gitWrapper = new GitWrapper(verbose: true);
            var projectName = GitWrapper.ProjectNameFromUrl(ValidUrl);
            var projectPath = Path.Combine(TempSourceDir, projectName);
            Directory.CreateDirectory(projectPath); // Simulate existing project directory

            // Act
            var result = gitWrapper.CloneProject(ValidUrl, TempSourceDir);

            // Assert
            Assert.IsFalse(result.IsSuccess(), "CloneProject should fail if the project already exists.");
            Assert.AreEqual((int)RetCode.CloneProjectFailed, result.Code, "Expected CloneProjectFailed error code.");
        }

        [TestMethod]
        public void CloneProject_ShouldCreateSourceDir_IfNotExists()
        {
            // Arrange
            var gitWrapper = new GitWrapper(verbose: true);
            var nonExistentSourceDir = Path.Combine(TempSourceDir, GenerateRandomString(5));

            // Act
            var result = gitWrapper.CloneProject(ValidUrl, nonExistentSourceDir);

            // Assert
            Assert.IsTrue(result.IsSuccess(), "CloneProject should succeed when the source directory does not exist.");
            Assert.IsTrue(Directory.Exists(nonExistentSourceDir), "The source directory should be created.");
        }

        [TestMethod()]
        public void GetCurrentBranchTest()
        {
            Assert.AreNotEqual(string.Empty, GitWrapper.Branch);
        }

        [TestMethod()]
        public void GetCurrentTagTest()
        {
            // Arrange
            var tag = InitTag();
            if (string.IsNullOrEmpty(tag))
            {
                tag = "1.0.0";
                Assert.IsTrue(GitWrapper.SetTag(tag));
            }

            // Act
            var currentTag = GitWrapper.Tag;

            // Assert
            Assert.AreNotEqual(string.Empty, currentTag);
        }

        [TestMethod, TestCategory("Manual"), Ignore("test because it is failing when run in GitHub Actions")]
        public void SetAutoTagTest()
        {
            // Arrange
            var buildTypes = new List<string>
            {
                Enums.BuildType.STAGE.ToString(), Enums.BuildType.PROD.ToString()
            };

            Console.WriteLine("Test for SetAutoTagTest");
            foreach (var buildType in buildTypes)
            {
                var test = Enums.BuildType.TryParse<Enums.BuildType>(buildType, true, out var buildTypeOut);

                Assert.IsTrue(test);
                Assert.AreEqual(buildType, buildTypeOut.ToString());

                Console.WriteLine($"Current tag: {GitWrapper.Tag}");
                var tag = GitWrapper.SetAutoTag(buildType);
                Console.WriteLine($"expected Tag: {tag}, buildType: {buildType}");

                Assert.AreEqual(tag, GitWrapper.Tag);
            }
        }

        [TestMethod()]
        public void IsValidTagTest()
        {
            Console.WriteLine("Test for valid tags");
            // Arrange for valid
            var ListOfValidTags = new List<string>
            {
                "1.1.1", "9999.999.000", "1.1.1", "11.1.1"
            };

            // Act and Assert that all tags are valid
            foreach (var tag in ListOfValidTags)
            {
                Console.WriteLine(tag);
                Assert.IsTrue(GitWrapper.IsValidTag(tag));
            }
            // Arrange for invalid tags
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var ListOfInvalidTags = new List<string>
            {
                "A.1.1.1", "9.b.1.1", "20.1.d.1", "1.1.mk", "1.P.1.1", "1lkjllkajsk1", "-1,0,0,0", "", null
            };
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            Console.WriteLine("Test for invalid tags");
            // Act and Assert that all tags are invalid
            foreach (var tag in ListOfInvalidTags)
            {
                Console.WriteLine(tag);
                Assert.IsFalse(GitWrapper.IsValidTag(tag));
            }
        }

        [TestMethod()]
        public void StageTagTest()
        {
            // Arrange Act and Assert.   Repeat for 1 time to speed up the test
            var times = 1;
            for (int i = 0; i < times; i++)
            {
                // Arrange
                var tag = GitWrapper.Tag;
                var expected = int.Parse(tag.Split('.')[2]) + 1; // last digit incremented by 1

                // take tag and change last digit to expected
                var expectedTag = $"{tag.Split('.')[0]}.{tag.Split('.')[1]}.{expected}";
                Console.WriteLine($"Tag - Current: {tag} Expected Tag: {expectedTag}");

                // Act
                var nextTag = GitWrapper.StageTag();
                Assert.IsTrue(GitWrapper.IsValidTag(nextTag));

                // Assert 
                Assert.AreEqual(expectedTag, nextTag);

                // Set tag to nextTag
                Assert.IsTrue(GitWrapper.SetTag(nextTag));

                // Set tag to nextTag
                //Assert.IsTrue(GitWrapper.PushTag(nextTag));

                // Sleep for 750ms to allow the tag to be set
                //Thread.Sleep(750);
            }
        }

        [TestMethod()]
        public void ProdTagTest()
        {
            // Arrange Act and Assert.   Repeat for 1 time to speed up the test
            var times = 1;
            for (int i = 0; i < times; i++)
            {
                // Arrange
                var tag = GitWrapper.Tag;
                var expected = int.Parse(tag.Split('.')[1]) + 1; // middle digit incremented by 1

                // take tag and change last digit to expected
                var expectedTag = $"{tag.Split('.')[0]}.{expected}.0";
                Console.WriteLine($"Tag - Current: {tag} Expected Tag: {expectedTag}");

                // Act
                var nextTag = GitWrapper.ProdTag();
                Assert.IsTrue(GitWrapper.IsValidTag(nextTag));

                // Assert 
                Assert.AreEqual(expectedTag, nextTag);

                // Set tag to nextTag
                Assert.IsTrue(GitWrapper.SetTag(nextTag));
            }
        }

        private string InitTag()
        {
            // Arrange
            var tag = GitWrapper.Tag;
            if (string.IsNullOrEmpty(tag))
            {
                tag = "1.0.0";
                Assert.IsTrue(GitWrapper.SetTag(tag));
            }

            return tag;
        }

        // Ignore this test because it is failing when run in GitHub Actions
        [TestMethod, TestCategory("Manual"), Ignore("This test is intended to be run manually because it fails in GitHub Actions.")]
        public void DeleteTagTest()
        {
            // Arrange
            var initialTag = InitTag();
            var currentTag = GitWrapper.Tag;
            Assert.IsNotNull(currentTag);

            // Act
            var result = GitWrapper.DeleteTag(currentTag);

            // Assert
            Assert.IsTrue(result);

            // reinitialize tag in case no tags are left
            Assert.IsNotNull(InitTag());
        }

        [TestMethod()]
        public void ListBranchesTest()
        {
            Assert.AreNotEqual(0, GitWrapper.ListBranches().Count);
        }

        [TestMethod()]
        public void CheckoutBranchTest2()
        {
            // Arrange
            var branch = "main";

            // Act
            var result = GitWrapper.CheckoutBranch(branch);
            Assert.IsNotNull(result);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod, TestCategory("Manual")]
        public void PushTagTest()
        {
            // Arrange
            var tag = GitWrapper.Tag;
            Assert.IsTrue(GitWrapper.SetTag(tag));
            // Arrange add a tag
            tag = GitWrapper.SetAutoTag(Enums.BuildType.STAGE.ToString());
            
            Assert.IsNotNull(tag);

            // Act
            var result = GitWrapper.PushTag(tag);

            // Assert
            if (GitHubActions)
            {
                Assert.Inconclusive();
            }
            else
            {
                Assert.IsTrue(result, " GitWrapper.PushTag(tag) returned false");
            }
        }

        // This test is time consuming and should be run manually
        [TestMethod, TestCategory("Manual")]
        public void ListRemoteTagsTest()
        {
            // Arrange add a tag
            var tag = GitWrapper.SetAutoTag(Enums.BuildType.STAGE.ToString());
            Assert.IsTrue(GitWrapper.SetTag(tag));
            GitWrapper.PushTag(tag);

            var remoteTags = GitWrapper.ListRemoteTags();
            Assert.IsNotNull(remoteTags);

            // Act
            Console.WriteLine("Remote Tags:");
            foreach (var tagItem in remoteTags)
            {
                Console.WriteLine(tagItem);
            }

            // Assert
            Assert.AreNotEqual(0, remoteTags.Count);
        }

        [TestMethod, TestCategory("Manual"), Ignore("This test is intended to be run manually because it is time consuming.")]
        public void ListLocalTagsTest()
        {
            // Arrange add a tag
            var localTags = GitWrapper.ListLocalTags();
            var tag = GitWrapper.SetAutoTag(Enums.BuildType.STAGE.ToString());
            Assert.IsTrue(GitWrapper.SetTag(tag));

            var expectedCount = localTags.Count + 1;
            // Act
            localTags = GitWrapper.ListLocalTags();
            Assert.IsNotNull(localTags);

            // Assert
            Assert.AreEqual(expectedCount, localTags.Count);
        }

        [TestMethod()]
        public void SetWorkingDirTest()
        {
            // Arrange
            var gitWrapper = new GitWrapper(project: null, verbose: true, testMode:true);

            var workingDir = ProjectName;
            var solutionDir = $@"{gitWrapper.DevDrive}\{gitWrapper.MainDir}\{ProjectName}";

            // Act
            var result = gitWrapper.SetWorkingDir(workingDir);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(solutionDir, gitWrapper.WorkingDirectory);
        }

        [TestMethod()]
        public void GetGitUserNameConfigurationTest()
        {
            // Arrange
            var gitWrapper = new GitWrapper(project: null, verbose: true, testMode: true);

            // Act
            var result = gitWrapper.GetGitUserNameConfiguration();

            // Assert
            // if running in GitHub Actions, git Email is not configured, if running locally, git is configured
            // ignore the test if running in GitHub Actions
            if (GitHubActions)
            {
                Assert.Inconclusive();
            }
            else
            {
                Assert.IsNotNull(result);
            }
        }

        [TestMethod()]
        public void GetGitUserEmailConfigurationTest()
        {
            // Arrange
            var gitWrapper = new GitWrapper(project: null, verbose: true, testMode: true);

            // Act
            var result = gitWrapper.GetGitUserEmailConfiguration();

            // Assert
            // if running in GitHub Actions, git Email is not configured, if running locally, git is configured
            // ignore the test if running in GitHub Actions
            if (GitHubActions)
            {
                Assert.Inconclusive();
            }
            else
            {
                Assert.IsNotNull(result);
            }
        }

        [TestMethod()]
        public void IsGitConfiguredTest()
        {
            // Arrange
            var gitWrapper = new GitWrapper(project: null, verbose: true);

            // Act
            var result = gitWrapper.IsGitConfigured();

            // Assert
            // if running in GitHub Actions, git UserName is not configured, if running locally, git is configured
            // ignore the test if running in GitHub Actions
            if (GitHubActions)
            {
                Assert.Inconclusive();
            }
            else
            {
                Assert.IsTrue(result);
            }
        }
    }
}