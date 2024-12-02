using NbuildTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;


namespace NbuildTasks.Tests
{
    [TestClass()]
    public class GitWrapperTests : TestFirst
    {
        private readonly GitWrapper GitWrapper = new(ProjectName, verbose: true, testMode: true);

        public GitWrapperTests()
        {
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"GitWrapper.Parameters.WorkingDir: {GitWrapper.WorkingDirectory}");
            //Assert.AreEqual(GitWrapper.WorkingDirectory, Directory.GetCurrentDirectory());
            Console.WriteLine($"Current Branch: {GitWrapper.Branch}");
            Console.WriteLine($"Current Tag: {GitWrapper.Tag}");
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
            // Arrange add a tag
            var tag = GitWrapper.SetAutoTag(Enums.BuildType.STAGE.ToString());
            Assert.IsTrue(GitWrapper.SetTag(tag));
            Assert.IsNotNull(tag);

            // Act
            var result = GitWrapper.PushTag(tag);

            // Assert
            if (!GitHubActions)
            {
                Assert.IsTrue(result, " GitWrapper.PushTag(tag) returned falase");
            }
            else
            {
                Assert.Inconclusive();
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
            if (!GitHubActions)
            {
                Assert.IsNotNull(result);
            }
            else
            {
                Assert.Inconclusive();
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
            if (!GitHubActions)
            {
                Assert.IsNotNull(result);
            }
            else
            {
                Assert.Inconclusive();
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
            if (!GitHubActions)
            {
                Assert.IsTrue(result);
            }
            else
            {
                Assert.Inconclusive();
            }
        }
    }
}