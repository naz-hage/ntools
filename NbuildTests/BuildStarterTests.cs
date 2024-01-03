﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Nbuild.Tests
{
    [TestClass()]
    public class BuildStarterTests
    {
        [TestMethod()]
        public void GetTargetsTest()
        {
            // Add test code here
            // Arrange
            List<string> expected =
            [
                "PROPERTIES",
                "CLEAN",
                "STAGING",
                "PRODUCTION",
                "STAGING_DEPLOY",
                "PRODUCTION_DEPLOY",
                "SOLUTION",
                "SOLUTION_MSBUILD",
                "PACKAGE",
                "SAVE_ARTIFACTS",
                "DEPLOY",
                "TEST",
                "TEST_RELEASE",
                "IS_ADMIN",
                "GIT_STATUS",
                "AUTOTAG_STAGING",
                "SET_TAG",
                "GIT_PULL",
                "AUTOTAG_PRODUCTION",
                "GET_TAG",
                "PUSH_TAG",
                "GIT_BRANCH",
                "SingleProject",
                "HandleError"];
    
            // Act
            var executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);
            string resourcePath = Path.Combine(executingAssemblyDirectory, "Nbuild.dll");
            string targetFileName = Path.Combine(executingAssemblyDirectory, "commom.targets");
    
            ResourceHelper.ExtractEmbeddedResource(resourcePath, "Nbuild.resources.common.targets", targetFileName);
    
            // Assert
            Console.WriteLine($"ResourcePath: {targetFileName}");
            Assert.IsTrue(File.Exists(targetFileName));
            var actual = BuildStarter.GetTargets(targetFileName).ToArray();

            Console.WriteLine($"Actual Targets:");
            Console.WriteLine($"{string.Join(",\n", actual.Select(a => $"\"{a}\""))}");

            // assert that arrays actual and expected are equal
            CollectionAssert.AreEqual(expected, actual, StringComparer.OrdinalIgnoreCase);
        }
    }
}