using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ntools;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static NbuildTasks.Enums;

namespace NbuildTasks.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class GitWrapperTests
    {
        private const string InvalidUrl = "invalid-url";
        private static readonly string ProjectName = "getting-started-" + Guid.NewGuid().ToString("N");
        private static string FixtureRoot;
        private static string BareRepository;
        private static string RepositoryUrl;

        private GitWrapper GitWrapper => new(ProjectName, verbose: true, testMode: true);

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            FixtureRoot = Path.Combine(Path.GetTempPath(), "NbuildTasksTests", "git-fixture-" + Guid.NewGuid().ToString("N"));
            var sourceRepository = Path.Combine(FixtureRoot, "source");
            BareRepository = Path.Combine(FixtureRoot, ProjectName + ".git");
            RepositoryUrl = new Uri(BareRepository).AbsoluteUri;

            Directory.CreateDirectory(FixtureRoot);
            RunGit(FixtureRoot, "init --bare " + Quote(BareRepository));
            RunGit(FixtureRoot, "init -b main " + Quote(sourceRepository));
            RunGit(sourceRepository, "config user.name nbuild-tests");
            RunGit(sourceRepository, "config user.email nbuild-tests@example.invalid");
            File.WriteAllText(Path.Combine(sourceRepository, "README.md"), "local GitWrapper fixture");
            RunGit(sourceRepository, "add README.md");
            RunGit(sourceRepository, "commit -m initial");
            RunGit(sourceRepository, "tag 1.0.0");
            RunGit(sourceRepository, "remote add origin " + Quote(BareRepository));
            RunGit(sourceRepository, "push --set-upstream origin main");
            RunGit(sourceRepository, "push origin 1.0.0");
            RunGit(BareRepository, "symbolic-ref HEAD refs/heads/main");

            var workingRepository = Path.Combine(Path.GetTempPath(), "NbuildTasksTests", ProjectName);
            DeleteDirectory(workingRepository);
            RunGit(Path.GetDirectoryName(workingRepository), "clone --branch main " + Quote(BareRepository) + " " + Quote(workingRepository));
            RunGit(workingRepository, "config user.name nbuild-tests");
            RunGit(workingRepository, "config user.email nbuild-tests@example.invalid");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _ = new GitWrapper(project: null, testMode: true);
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            DeleteDirectory(FixtureRoot);
            DeleteDirectory(Path.Combine(Path.GetTempPath(), "NbuildTasksTests", ProjectName));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            Directory.SetCurrentDirectory(Path.Combine(Path.GetTempPath(), "NbuildTasksTests", ProjectName));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }

        [TestMethod]
        public void CloneProject_ShouldSucceed_WithValidUrl()
        {
            var sourceDirectory = CreateTestDirectory();
            var result = new GitWrapper(verbose: true).CloneProject(RepositoryUrl, sourceDirectory);

            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(Directory.Exists(Path.Combine(sourceDirectory, ProjectName)));
        }

        [TestMethod]
        public void CloneProject_ShouldFail_WithInvalidUrl()
        {
            var result = new GitWrapper(verbose: true).CloneProject(InvalidUrl, CreateTestDirectory());

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(ResultHelper.InvalidParameter, result.Code);
        }

        [TestMethod]
        public void CloneProject_ShouldFail_WhenProjectAlreadyExists()
        {
            var sourceDirectory = CreateTestDirectory();
            Directory.CreateDirectory(Path.Combine(sourceDirectory, ProjectName));

            var result = new GitWrapper(verbose: true).CloneProject(RepositoryUrl, sourceDirectory);

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual((int)RetCode.CloneProjectFailed, result.Code);
        }

        [TestMethod]
        public void CloneProject_ShouldCreateSourceDir_IfNotExists()
        {
            var sourceDirectory = Path.Combine(CreateTestDirectory(), "nested");
            var result = new GitWrapper(verbose: true).CloneProject(RepositoryUrl, sourceDirectory);

            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(Directory.Exists(sourceDirectory));
        }

        [TestMethod]
        public void GetCurrentBranchTest() => Assert.AreEqual("main", GitWrapper.Branch);

        [TestMethod]
        public void GetCurrentTagTest() => Assert.AreEqual("1.0.0", GitWrapper.Tag);

        [TestMethod]
        public void SetAutoTagTest()
        {
            var tag = GitWrapper.SetAutoTag(BuildType.STAGE.ToString());

            Assert.AreEqual(tag, GitWrapper.Tag);
            Assert.IsTrue(GitWrapper.IsValidTag(tag));
        }

        [TestMethod]
        public void IsValidTagTest()
        {
            foreach (var tag in new[] { "1.1.1", "9999.999.000", "11.1.1" })
            {
                Assert.IsTrue(GitWrapper.IsValidTag(tag));
            }

            foreach (var tag in new[] { "A.1.1.1", "9.b.1.1", "1.1.mk", "", null })
            {
                Assert.IsFalse(GitWrapper.IsValidTag(tag));
            }
        }

        [TestMethod]
        public void StageTagTest()
        {
            var currentTag = GitWrapper.Tag;
            var expectedTag = $"{currentTag.Split('.')[0]}.{currentTag.Split('.')[1]}.{int.Parse(currentTag.Split('.')[2]) + 1}";
            var nextTag = GitWrapper.StageTag();

            Assert.AreEqual(expectedTag, nextTag);
            Assert.IsTrue(GitWrapper.IsValidTag(nextTag));
        }

        [TestMethod]
        public void ProdTagTest()
        {
            var currentTag = GitWrapper.Tag;
            var expectedTag = $"{currentTag.Split('.')[0]}.{int.Parse(currentTag.Split('.')[1]) + 1}.0";
            var nextTag = GitWrapper.ProdTag();

            Assert.AreEqual(expectedTag, nextTag);
            Assert.IsTrue(GitWrapper.IsValidTag(nextTag));
        }

        [TestMethod]
        public void DeleteTagTest()
        {
            var tag = GitWrapper.Tag;

            Assert.IsTrue(GitWrapper.DeleteTag(tag));
            Assert.IsFalse(GitWrapper.DeleteTag(tag));
        }

        [TestMethod]
        public void ListBranchesTest() => Assert.IsTrue(GitWrapper.ListBranches().Contains("main"));

        [TestMethod]
        public void CheckoutBranchTest2() => Assert.IsTrue(GitWrapper.CheckoutBranch("main"));

        [TestMethod]
        public void PushTagTest()
        {
            var tag = GitWrapper.SetAutoTag(BuildType.STAGE.ToString());

            Assert.IsTrue(GitWrapper.PushTag(tag));
            Assert.IsTrue(GitWrapper.ListRemoteTags().Contains(tag));
        }

        [TestMethod]
        public void ListRemoteTagsTest() => Assert.IsTrue(GitWrapper.ListRemoteTags().Contains("1.0.0"));

        [TestMethod]
        public void ListLocalTagsTest() => Assert.IsTrue(GitWrapper.ListLocalTags().Contains("1.0.0"));

        [TestMethod]
        public void SetWorkingDirTest()
        {
            var wrapper = new GitWrapper(project: null, verbose: true, testMode: true);

            Assert.IsTrue(wrapper.SetWorkingDir(RepositoryUrl));
            Assert.AreEqual(Path.Combine(wrapper.SourceDir, ProjectName), wrapper.WorkingDirectory);
        }

        [TestMethod]
        public void GetGitUserNameConfigurationTest() => Assert.IsFalse(string.IsNullOrWhiteSpace(GitWrapper.GetGitUserNameConfiguration()));

        [TestMethod]
        public void GetGitUserEmailConfigurationTest() => Assert.IsFalse(string.IsNullOrWhiteSpace(GitWrapper.GetGitUserEmailConfiguration()));

        [TestMethod]
        public void IsGitConfiguredTest() => Assert.IsTrue(GitWrapper.IsGitConfigured());

        private static string CreateTestDirectory()
        {
            var path = Path.Combine(FixtureRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void RunGit(string workingDirectory, string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Assert.IsNotNull(process);
            process.WaitForExit();
            var error = process.StandardError.ReadToEnd();
            Assert.AreEqual(0, process.ExitCode, $"git {arguments} failed: {error}");
        }

        private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, true);
            }
        }
    }
}
