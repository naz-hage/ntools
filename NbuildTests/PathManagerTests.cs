// -----------------------------------------------------------------------------
// File: PathManagerTests.cs
// Purpose: Unit tests for PathManager class to ensure safe and correct PATH
//          environment variable manipulation.
// -----------------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild.Services;
using System;

namespace NbuildTests
{
    [TestClass]
    public class PathManagerTests
    {
        private PathSnapshot? _snapshot;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a snapshot of the current PATH before each test
            _snapshot = PathManager.CreateSnapshot();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore the original PATH after each test
            if (_snapshot != null)
            {
                PathManager.RestoreSnapshot(_snapshot);
            }
        }

        [TestMethod]
        public void GetUserPath_ReturnsCurrentPath()
        {
            // Act
            var path = PathManager.GetUserPath();

            // Assert
            Assert.IsNotNull(path);
            // We can't assert exact content since it varies by environment,
            // but we can verify it's a string
        }

        [TestMethod]
        public void SetUserPath_UpdatesPathCorrectly()
        {
            // Arrange
            var testPath = @"C:\TestPath1;C:\TestPath2";

            // Act
            PathManager.SetUserPath(testPath);
            var result = PathManager.GetUserPath();

            // Assert
            Assert.AreEqual(testPath, result);
        }

        [TestMethod]
        public void SetUserPath_NullOrEmpty_ClearsPath()
        {
            // Arrange
            PathManager.SetUserPath(@"C:\Temp");

            // Act & Assert
            PathManager.SetUserPath(null);
            Assert.AreEqual(string.Empty, PathManager.GetUserPath());

            PathManager.SetUserPath(string.Empty);
            Assert.AreEqual(string.Empty, PathManager.GetUserPath());
        }

        [TestMethod]
        public void AddPath_NewPath_PrependsToExistingPath()
        {
            // Arrange
            var originalPath = @"C:\Windows;C:\Program Files";
            PathManager.SetUserPath(originalPath);
            var newPath = @"C:\NewPath";

            // Act
            PathManager.AddPath(newPath);
            var result = PathManager.GetUserPath();

            // Assert
            Assert.AreEqual($@"{newPath};{originalPath}", result);
        }

        [TestMethod]
        public void AddPath_EmptyPath_NoChange()
        {
            // Arrange
            var originalPath = @"C:\Windows";
            PathManager.SetUserPath(originalPath);

            // Act
            PathManager.AddPath(string.Empty);
            PathManager.AddPath(null);
            PathManager.AddPath("   ");

            // Assert
            Assert.AreEqual(originalPath, PathManager.GetUserPath());
        }

        [TestMethod]
        public void AddPath_ExistingPath_NoDuplicate()
        {
            // Arrange
            var originalPath = @"C:\NewPath;C:\Windows";
            PathManager.SetUserPath(originalPath);

            // Act
            PathManager.AddPath(@"C:\NewPath");

            // Assert
            Assert.AreEqual(originalPath, PathManager.GetUserPath());
        }

        [TestMethod]
        public void AddPath_CaseInsensitive_NoDuplicate()
        {
            // Arrange
            PathManager.SetUserPath(@"C:\Windows");

            // Act
            PathManager.AddPath(@"c:\windows");

            // Assert
            var segments = PathManager.GetPathSegments();
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(@"C:\Windows", segments[0]);
        }

        [TestMethod]
        public void RemovePath_ExistingPath_RemovesCorrectly()
        {
            // Arrange
            var originalPath = @"C:\NewPath;C:\Windows;C:\Program Files";
            PathManager.SetUserPath(originalPath);

            // Act
            PathManager.RemovePath(@"C:\Windows");
            var result = PathManager.GetUserPath();

            // Assert
            Assert.AreEqual(@"C:\NewPath;C:\Program Files", result);
        }

        [TestMethod]
        public void RemovePath_NonExistingPath_NoChange()
        {
            // Arrange
            var originalPath = @"C:\Windows;C:\Program Files";
            PathManager.SetUserPath(originalPath);

            // Act
            PathManager.RemovePath(@"C:\NonExistent");

            // Assert
            Assert.AreEqual(originalPath, PathManager.GetUserPath());
        }

        [TestMethod]
        public void RemovePath_CaseInsensitive_RemovesCorrectly()
        {
            // Arrange
            PathManager.SetUserPath(@"C:\Windows;C:\Program Files");

            // Act
            PathManager.RemovePath(@"c:\windows");

            // Assert
            Assert.AreEqual(@"C:\Program Files", PathManager.GetUserPath());
        }

        [TestMethod]
        public void RemovePath_EmptyPath_NoChange()
        {
            // Arrange
            var originalPath = @"C:\Windows";
            PathManager.SetUserPath(originalPath);

            // Act
            PathManager.RemovePath(string.Empty);
            PathManager.RemovePath(null);
            PathManager.RemovePath("   ");

            // Assert
            Assert.AreEqual(originalPath, PathManager.GetUserPath());
        }

        [TestMethod]
        public void GetPathSegments_ValidPath_ReturnsCorrectSegments()
        {
            // Arrange
            var testPath = @"C:\Windows;C:\Program Files;C:\Temp";

            // Act
            var segments = PathManager.GetPathSegments(testPath);

            // Assert
            Assert.AreEqual(3, segments.Length);
            Assert.AreEqual(@"C:\Windows", segments[0]);
            Assert.AreEqual(@"C:\Program Files", segments[1]);
            Assert.AreEqual(@"C:\Temp", segments[2]);
        }

        [TestMethod]
        public void GetPathSegments_NullOrEmptyPath_ReturnsEmptyArray()
        {
            // Act & Assert
            Assert.AreEqual(0, PathManager.GetPathSegments(string.Empty).Length);
            Assert.AreEqual(0, PathManager.GetPathSegments("   ").Length);
        }

        [TestMethod]
        public void GetPathSegments_PathWithEmptySegments_FiltersEmpty()
        {
            // Arrange
            var testPath = @"C:\Windows;;C:\Program Files;;C:\Temp";

            // Act
            var segments = PathManager.GetPathSegments(testPath);

            // Assert
            Assert.AreEqual(3, segments.Length);
            Assert.AreEqual(@"C:\Windows", segments[0]);
            Assert.AreEqual(@"C:\Program Files", segments[1]);
            Assert.AreEqual(@"C:\Temp", segments[2]);
        }

        [TestMethod]
        public void DeduplicateAndRewrite_PathWithDuplicates_RemovesDuplicates()
        {
            // Arrange
            var testPath = @"C:\Windows;C:\Program Files;C:\Windows;C:\Temp;C:\Program Files";

            // Act
            var result = PathManager.DeduplicateAndRewrite(testPath);

            // Assert
            Assert.AreEqual(@"C:\Windows;C:\Program Files;C:\Temp", result);
        }

        [TestMethod]
        public void DeduplicateAndRewrite_PathWithCaseDuplicates_RemovesDuplicates()
        {
            // Arrange
            var testPath = @"C:\Windows;c:\windows;C:\Program Files";

            // Act
            var result = PathManager.DeduplicateAndRewrite(testPath);

            // Assert
            Assert.AreEqual(@"C:\Windows;C:\Program Files", result);
        }

        [TestMethod]
        public void DeduplicateAndRewrite_NullPath_UpdatesUserPath()
        {
            // Arrange
            var originalPath = @"C:\Windows;C:\Program Files;C:\Windows";
            PathManager.SetUserPath(originalPath);

            // Act
            var result = PathManager.DeduplicateAndRewrite();

            // Assert
            Assert.AreEqual(@"C:\Windows;C:\Program Files", result);
            Assert.AreEqual(@"C:\Windows;C:\Program Files", PathManager.GetUserPath());
        }

        [TestMethod]
        public void DeduplicateAndRewrite_NoDuplicates_ReturnsSamePath()
        {
            // Arrange
            var testPath = @"C:\Windows;C:\Program Files;C:\Temp";

            // Act
            var result = PathManager.DeduplicateAndRewrite(testPath);

            // Assert
            Assert.AreEqual(testPath, result);
        }

        [TestMethod]
        public void CreateSnapshot_CapturesCurrentPath()
        {
            // Arrange
            var testPath = @"C:\TestSnapshot";
            PathManager.SetUserPath(testPath);

            // Act
            var snapshot = PathManager.CreateSnapshot();

            // Assert
            Assert.AreEqual(testPath, snapshot.OriginalPath);
        }

        [TestMethod]
        public void RestoreSnapshot_RecoversOriginalPath()
        {
            // Arrange
            var originalPath = @"C:\Original";
            var modifiedPath = @"C:\Modified";
            PathManager.SetUserPath(originalPath);
            var snapshot = PathManager.CreateSnapshot();
            PathManager.SetUserPath(modifiedPath);

            // Act
            PathManager.RestoreSnapshot(snapshot);

            // Assert
            Assert.AreEqual(originalPath, PathManager.GetUserPath());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestoreSnapshot_NullSnapshot_ThrowsException()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            PathManager.RestoreSnapshot(null);
#pragma warning restore CS8625
        }

        [TestMethod]
        public void PathSnapshot_Constructor_HandlesNullPath()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            var snapshot = new PathSnapshot(null);
#pragma warning restore CS8625

            // Assert
            Assert.AreEqual(string.Empty, snapshot.OriginalPath);
        }

        [TestMethod]
        public void Integration_AddRemovePath_WorksCorrectly()
        {
            // Arrange
            PathManager.SetUserPath(@"C:\Windows");

            // Act: Add a path
            PathManager.AddPath(@"C:\NewPath");
            Assert.AreEqual(@"C:\NewPath;C:\Windows", PathManager.GetUserPath());

            // Act: Try to add the same path again (should be idempotent)
            PathManager.AddPath(@"C:\NewPath");
            Assert.AreEqual(@"C:\NewPath;C:\Windows", PathManager.GetUserPath());

            // Act: Remove the path
            PathManager.RemovePath(@"C:\NewPath");
            Assert.AreEqual(@"C:\Windows", PathManager.GetUserPath());

            // Act: Try to remove non-existent path (should be idempotent)
            PathManager.RemovePath(@"C:\NonExistent");
            Assert.AreEqual(@"C:\Windows", PathManager.GetUserPath());
        }
    }
}