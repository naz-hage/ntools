using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuildTasks;
using System.Reflection;

namespace NbuildTests
{
    [TestClass()]
    public class NtoolsJsonTests
    {
        private const string NbuildAssemblyName = "Nb.dll"; // "Nbuild.dll"
        [TestMethod()]
        public void ValidateNtoolsJsonTest()
        {
            // Arrange
            string resourceLocation = "Nbuild.resources.ntools.json";
            string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            string targetFileName = Path.Combine(executingAssemblyDirectory, "ntools.json");
            var assembly = Path.Combine(executingAssemblyDirectory, NbuildAssemblyName);

            // Act
            ResourceHelper.ExtractEmbeddedResourceFromAssembly(assembly, resourceLocation, targetFileName);

            // Assert
            Console.WriteLine($"ResourcePath: {targetFileName}");
            Assert.IsTrue(File.Exists(targetFileName));
        }
    }
}