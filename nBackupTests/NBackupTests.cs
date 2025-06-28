using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbackup;
using NbuildTasks;
using Ntools;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace NbackupTests
{
    [TestClass()]
    public class NBackupTests
    {
        private static string GetEmbeddedResourcePath(string assemblyPath, string resourceFileName)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Assembly not found at: {assemblyPath}");
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            var resourceNames = assembly.GetManifestResourceNames();

            // Debug: Print available resources
            Console.WriteLine($"Available resources in {Path.GetFileName(assemblyPath)}:");
            foreach (var name in resourceNames)
            {
                Console.WriteLine($"  - {name}");
            }

            // Try to find the resource that contains our target file name
            var matchingResource = resourceNames.FirstOrDefault(r => r.EndsWith(resourceFileName, StringComparison.OrdinalIgnoreCase));

            if (matchingResource == null)
            {
                throw new InvalidOperationException($"No resource found ending with '{resourceFileName}'. Available resources: {string.Join(", ", resourceNames)}");
            }

            return matchingResource;
        }

        [TestMethod()]
        public void PerformTest()
        {
            // Arrange
            Assembly? assembly = Assembly.GetAssembly(typeof(NBackupTests));
            Assert.IsNotNull(assembly);
            string nbackup = $"{Path.GetDirectoryName(assembly.Location)}\\Nbackup.dll";
            var backupInput = $"{Path.GetDirectoryName(assembly.Location)}\\backup.json";

            try
            {
                var correctResourceName = GetEmbeddedResourcePath(nbackup, "Nbackup.json");
                ResourceHelper.ExtractEmbeddedResourceFromAssembly(nbackup, correctResourceName, backupInput);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to extract embedded resource: {ex.Message}");
            }

            // Act
            Assert.IsTrue(Parser.TryParse($"-i {backupInput}", out Cli options));
            Console.WriteLine(backupInput);
            ResultHelper result = NBackup.Perform(options);

            // Assert
            Assert.IsFalse(result.Code > 3);
        }

        [TestMethod()]
        public void PerformTestInvalidInput()
        {
            // Arrange
            string backupInput = "\\..\\nBackup\\Data\\TextFile.txt";

            Assert.IsTrue(Parser.TryParse($"-i {backupInput}", out Cli options));

            // Act
            ResultHelper result = NBackup.Perform(options);

            // Assert
            Assert.AreEqual(ResultHelper.FileNotFound, result.Code);
        }

        [TestMethod()]
        public void PerformTestInvalidInputBadFormattedJson()
        {
            Assert.IsTrue(Parser.TryParse($"-i m", out Cli options));

            ResultHelper result = NBackup.Perform(options);
            Assert.AreEqual(ResultHelper.FileNotFound, result.Code);
        }

        [TestMethod()]
        public void PerformTestValidInput()
        {
            Assembly? assembly = Assembly.GetAssembly(typeof(NBackupTests));
            Assert.IsNotNull(assembly);
            string nbackup = $"{Path.GetDirectoryName(assembly.Location)}\\Nbackup.dll";
            var jsonFile = $"{Path.GetDirectoryName(assembly.Location)}\\backup.json";

            try
            {
                var correctResourceName = GetEmbeddedResourcePath(nbackup, "Nbackup.json");
                ResourceHelper.ExtractEmbeddedResourceFromAssembly(nbackup, correctResourceName, jsonFile);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to extract embedded resource: {ex.Message}");
            }

            Console.WriteLine($"file: {jsonFile}");
            var jsonString = File.ReadAllText(jsonFile);
            try
            {
                Backup? backups = JsonSerializer.Deserialize<Backup>(jsonString);
                Assert.IsNotNull(backups);
                Assert.IsNotNull(backups.BackupsList);
                Assert.AreEqual(1, backups.BackupsList.Count);
                Assert.AreEqual(".", backups.BackupsList[0].Source);
                Assert.AreEqual("%APPDATA%\\ntools", backups.BackupsList[0].Destination);
                Assert.AreEqual("/V /R:5 /W:5 /MT:16 /dcopy:DAT /copy:DT", backups.BackupsList[0].BackupOptions);
                Assert.IsNotNull(backups.BackupsList[0].ExcludeFolders);
                Assert.AreEqual(2, backups.BackupsList[0].ExcludeFolders?.Count ?? 0);
                Assert.IsNotNull(backups.BackupsList[0].ExcludeFiles);
                Assert.AreEqual(3, backups.BackupsList[0].ExcludeFiles?.Count ?? 0);
                Assert.AreEqual("%APPDATA%\\backup.log", backups.BackupsList[0].LogFile);
            }
            catch (JsonException)
            {
                Assert.Fail("Backup JSON is invalid. Deserialization failed.");
            }
        }
    }
}