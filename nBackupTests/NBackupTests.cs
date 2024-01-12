using CommandLine;
using Launcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuildTasks;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Nbackup.Tests
{
    [TestClass()]
    public class NBackupTests
    {
        private const string ResourceLocation = "Nbackup.Resources.Nbackup.json";

        [TestMethod()]
        public void PerformTest()
        {
            // Arrange
            Assembly? assembly = Assembly.GetAssembly(typeof(NBackupTests));
            Assert.IsNotNull(assembly);
            string nbackup = $"{Path.GetDirectoryName(assembly.Location)}\\Nbackup.dll";
            var backupInput = $"{Path.GetDirectoryName(assembly.Location)}\\backup.json";

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(nbackup, ResourceLocation, backupInput);

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

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(nbackup, ResourceLocation, jsonFile);

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