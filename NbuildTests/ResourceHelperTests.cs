using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuildTasks;
using System.Reflection;

namespace Nbuild.Tests
{
    [TestClass()]
    public class ResourceHelperTests
    {
        private const string NbuildAssemblyName = "Nb.dll"; // "Nbuild.dll"
        [TestMethod()]
        public void ExtractEmbeddedResourceFromAssemblyTest()
        {
            // Arrange
            string resourceLocation = "Nbuild.resources.common.targets";
            string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            string targetFileName = Path.Combine(executingAssemblyDirectory, "commom.targets");
            var assembly = Path.Combine(executingAssemblyDirectory, NbuildAssemblyName);

            // Act
            ResourceHelper.ExtractEmbeddedResourceFromAssembly(assembly, resourceLocation, targetFileName);

            // Assert
            Console.WriteLine($"ResourcePath: {targetFileName}");
            Assert.IsTrue(File.Exists(targetFileName));
        }
    }
}