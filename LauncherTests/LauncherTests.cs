﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Launcher.Tests
{
    [TestClass()]
    public class LauncherTests
    {
        [TestMethod]
        public void ProcessStartTestPass()
        {
            Parameters parameters = new()
            {
                WorkingDir = Directory.GetCurrentDirectory(),
                Arguments = "pass",
                FileName = "test.exe",
                RedirectStandardOutput = true
            };

            var result = Launcher.Start(parameters);
            Assert.AreEqual(0, result.Code);
            Assert.AreEqual(2, result.Output.Count);
        }

        [TestMethod]
        public void ProcessStartTestFail()
        {
            Parameters parameters = new()
            {
                WorkingDir = Directory.GetCurrentDirectory(),
                Arguments = "fail",
                FileName = "test.exe",
                RedirectStandardOutput = true
            };

            var result = Launcher.Start(parameters);


            Assert.AreEqual(-100, result.Code);
            Assert.AreEqual(5, result.Output.Count);
            Assert.IsTrue(result.Output.Contains("fail"));
            Assert.IsTrue(result.Output.Contains("error"));
            Assert.IsTrue(result.Output.Contains("rejected"));
        }

        [TestMethod()]
        public void LaunchInThreadTest()
        {
            var result = Launcher.LaunchInThread(
                           workingDir: Directory.GetCurrentDirectory(),
                           fileName: "test.exe",
                           arguments: "pass"
                           );
            Assert.AreEqual(0, result.Code);
            Assert.AreEqual("Success", result.Output[0]);

            result = Launcher.LaunchInThread(
               workingDir: Directory.GetCurrentDirectory(),
               fileName: "test1.exe",
               arguments: "fail"
               );
            Assert.AreEqual(int.MinValue, result.Code);
            Assert.IsTrue(result.Output[0].Contains("not found"));
        }

        [TestMethod()]
        public void ProcessStartTestWithLauncherParameters()
        {
            Parameters launcherParameters = new()
            {
                WorkingDir = Directory.GetCurrentDirectory(),
                FileName = "test.exe",
                Arguments = "fail",
                RedirectStandardOutput = true
            };

            var result = Launcher.Start(launcherParameters);

            Assert.AreEqual(-100, result.Code);
            Assert.AreEqual(5, result.Output.Count);
            Assert.IsTrue(result.Output.Contains("fail"));
            Assert.IsTrue(result.Output.Contains("error"));
            Assert.IsTrue(result.Output.Contains("rejected"));
        }
    }
}