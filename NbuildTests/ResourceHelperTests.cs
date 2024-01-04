﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Nbuild.Tests
{
    [TestClass()]
    public class ResourceHelperTests
    {
        [TestMethod()]
        public void ExtractEmbeddedResourceFromAssemblyTest()
        {
            // Arrange
            string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);
            
            string targetFileName = Path.Combine(executingAssemblyDirectory, "commom.targets");

            // Act
            ResourceHelper.ExtractEmbeddedResource("Nbuild.resources.common.targets", targetFileName);

            // Assert
            Console.WriteLine($"ResourcePath: {targetFileName}");
            Assert.IsTrue(File.Exists(targetFileName));
        }
    }
}