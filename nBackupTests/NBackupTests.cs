using CommandLine;
using launcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.Json;

namespace nbackup.Tests
{
    [TestClass()]
    public class NBackupTests
    {
        private readonly string backupInput = "..\\..\\..\\..\\nBackup\\Data\\backup.json";

        [TestMethod()]
        public void PerformTest()
        {
            Assert.IsTrue(Parser.TryParse($"-input {backupInput}", out Cli options));
            Console.WriteLine(backupInput);
            Result result = NBackup.Perform(options);
            Assert.IsFalse(result.Code > 2);
        }

        [TestMethod()]
        public void PerformTestInvalidInput()
        {
            string backupInput = "..\\..\\..\\..\\nBackup\\Data\\TextFile.txt";

            Assert.IsTrue(Parser.TryParse($"-input {backupInput}", out Cli options));

            Result result = NBackup.Perform(options);
            Assert.AreEqual(Result.FileNotFound, result.Code);
        }

        [TestMethod()]
        public void PerformTestInvalidInputBadFormattedJson()
        {
            Assert.IsTrue(Parser.TryParse($"-input m", out Cli options));

            Result result = NBackup.Perform(options);
            Assert.AreEqual(Result.FileNotFound, result.Code);
        }

        [TestMethod()]
        public void PerformTestInvalidInputBadFormattedJson2()
        {
            var file = Path.GetFullPath(backupInput);
            var jsonString = File.ReadAllText(file);
            try
            {
                Backup? backups = JsonSerializer.Deserialize<Backup>(jsonString);
                Assert.IsNotNull(backups);
                Assert.IsNotNull(backups.BackupsList);
                Assert.AreEqual(1, backups.BackupsList.Count);
                Assert.AreEqual("c:\\source\\naz-hage\\dev-ops", backups.BackupsList[0].Source);
                Assert.AreEqual("c:\\temp\\Users\\%USERNAME%\\dev-ops", backups.BackupsList[0].Destination);
                Assert.AreEqual("/S /V /R:5 /W:5 /MT:16 /dcopy:DAT /copy:DT", backups.BackupsList[0].BackupOptions);
                Assert.IsNotNull(backups.BackupsList[0].ExcludeFolders);
                Assert.AreEqual(2, backups.BackupsList[0].ExcludeFolders?.Count?? 0);
                Assert.IsNotNull(backups.BackupsList[0].ExcludeFiles);
                Assert.AreEqual(3, backups.BackupsList[0].ExcludeFiles?.Count ?? 0);
                Assert.AreEqual("c:\\temp\\backup.log", backups.BackupsList[0].LogFile);
            }
            catch (JsonException)
            {
                Assert.Fail("Backup JSON is invalid. Deserialization failed.");
            }
        }
    }
}