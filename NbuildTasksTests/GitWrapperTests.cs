using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace NbuildTasks.Tests
{
    [TestClass()]
    public class GitWrapperTests : TestFirst
    {
        private readonly GitWrapper GitWrapper = new GitWrapper(ProjectName);

        public GitWrapperTests()
        {
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"GitWrapper.Parameters.WorkingDir: {GitWrapper.Parameters.WorkingDir}");
            Assert.AreEqual(GitWrapper.Parameters.WorkingDir, Directory.GetCurrentDirectory());
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
            InitTag();

            var tag = GitWrapper.Tag;
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

        // Ignore this test because it is failing when run in GitHub Actions
        [TestMethod(), Ignore]
        public void SetAutoTagTest()
        {
            var buildTypes = new List<string>
            {
                "staging", "production"
            };

            Console.WriteLine("Test for SetAutoTagTest");
            foreach (var buildType in buildTypes)
            {
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
        public void StagingTagTest()
        {
            // Act and Assert that all tags are valid
            for (int i = 0; i < 10; i++)
            {
                // Arrange
                var tag = GitWrapper.Tag;
                var expected = int.Parse(tag.Split('.')[2]) + 1; // last digit incremented by 1

                // take tag and change last digit to expected
                var expectedTag = $"{tag.Split('.')[0]}.{tag.Split('.')[1]}.{expected}";
                Console.WriteLine($"Tag - Current: {tag} Expected Tag: {expectedTag}");

                // Act
                var nextTag = GitWrapper.StagingTag();
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
        public void ProductionTagTest()
        {
            // Act and Assert that all tags are valid
            // Act and Assert that all tags are valid
            for (int i = 0; i < 10; i++)
            {
                // Arrange
                var tag = GitWrapper.Tag;
                var expected = int.Parse(tag.Split('.')[1]) + 1; // middle digit incremented by 1

                // take tag and change last digit to expected
                var expectedTag = $"{tag.Split('.')[0]}.{expected}.0";
                Console.WriteLine($"Tag - Current: {tag} Expected Tag: {expectedTag}");

                // Act
                var nextTag = GitWrapper.ProductionTag();
                Assert.IsTrue(GitWrapper.IsValidTag(nextTag));

                // Assert 
                Assert.AreEqual(expectedTag, nextTag);

                // Set tag to nextTag
                Assert.IsTrue(GitWrapper.SetTag(nextTag));

                // Sleep for 750ms to allow the tag to be set
                //Thread.Sleep(750);

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
        [TestMethod, Ignore]
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

        [TestMethod()]
        public void ListRemoteTagsTest()
        {
            // Arrange add a tag
            var tag = GitWrapper.SetAutoTag("staging");
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

        [TestMethod()]
        public void ListLocalTagsTest()
        {
            // Arrange add a tag
            var tag = GitWrapper.SetAutoTag("staging");
            Assert.IsTrue(GitWrapper.SetTag(tag));

            var localTags = GitWrapper.ListLocalTags();
            Assert.IsNotNull(localTags);

            // Act
            Console.WriteLine("Local Tags:");
            foreach (var tagItem in localTags)
            {
                Console.WriteLine(tagItem);
            }

            // Assert
            Assert.AreNotEqual(0, localTags.Count);
        }
    }
}