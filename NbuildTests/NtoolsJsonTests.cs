using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using NbuildTasks;
using System.Reflection;
using System.Text.Json;

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

            ExtractToSingleAppJsonFile(targetFileName);
        }

        private void ExtractToSingleAppJsonFile(string targetFileName)
        {
            var json = File.ReadAllText(targetFileName);

            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            var listAppData = JsonSerializer.Deserialize<NbuildApps>(json) ?? throw new ParserException("Failed to parse json to list of objects", null);

            // make sure version matches 1.2.0
            if (listAppData.Version != "1.2.0")
            {
                throw new ParserException($"Version {listAppData.Version} is not supported. Please use version 1.2.0", null);
            }

            var appsFolder =@"c:\temp\apps"; 
            if (!Directory.Exists(appsFolder)) Directory.CreateDirectory(appsFolder);

            // create a new json file for each item in the json file and save it to the appsFolder directory
            foreach (var appData in listAppData.NbuildAppList)
            {
                var appsOne = new NbuildApps("1.2.0", [appData]);

                var jsonItem = JsonSerializer.Serialize(appsOne, new JsonSerializerOptions { WriteIndented = true });
                var jsonFilename = $"app-{appData.Name}.json";
                jsonFilename = jsonFilename.Replace(" ", "_");
                var fileName = Path.Combine(appsFolder, jsonFilename);
                File.WriteAllText(fileName, jsonItem);
                Console.WriteLine($"File: {fileName}");
            }
        }
    }
}