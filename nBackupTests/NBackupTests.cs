using CommandLine;
using Launcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace nbackup.Tests
{
    [TestClass()]
    public class NBackupTests
    {

        [TestMethod()]
        public void PerformTest()
        {
            string backupInput = "..\\..\\nBackup\\Data\\backup.json";
            Assert.IsTrue(Parser.TryParse($"-input {backupInput}", out Cli options));
            Console.WriteLine(backupInput);
            ResultHelper result = NBackup.Perform(options);
            Assert.IsFalse(result.Code > 3);
        }

        [TestMethod()]
        public void PerformTestInvalidInput()
        {
            string backupInput = "\\..\\nBackup\\Data\\TextFile.txt";

            Assert.IsTrue(Parser.TryParse($"-input {backupInput}", out Cli options));

            ResultHelper result = NBackup.Perform(options);
            Assert.AreEqual(ResultHelper.FileNotFound, result.Code);
        }

        [TestMethod()]
        public void PerformTestInvalidInputBadFormattedJson()
        {
            Assert.IsTrue(Parser.TryParse($"-input m", out Cli options));

            ResultHelper result = NBackup.Perform(options);
            Assert.AreEqual(ResultHelper.FileNotFound, result.Code);
        }

        [TestMethod()]
        public void PerformTestValidInput()
        {
            // Data\backup.json is in the same folder as the executable
            var file = $"{Path.GetDirectoryName(Assembly.GetAssembly(typeof(NBackupTests)).Location)}\\Data\\backup.json";

            Console.WriteLine($"file: {file}");
            var jsonString = File.ReadAllText(file);
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