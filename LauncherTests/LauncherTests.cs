using Launcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace Launcher.Tests
{



    [TestClass()]
    public class LauncherTests
    {
        private readonly Dictionary<string, int> TestResult = new Dictionary<string, int>()
        {
        {"pass", 2},
        {"fail", 5},
        };


        [TestMethod]
        public void ProcessStartTestPass()
        {
            (int result, List<string> lines) =
                            Launcher.Start(
                                workingDir: Directory.GetCurrentDirectory(),
                                fileName: "test.exe",
                                arguments: "pass",
                                redirectStandardOutput: true);
            Assert.AreEqual(0, result);
            Assert.AreEqual(2, lines.Count);
        }

        [TestMethod]
        public void ProcessStartTestFail()
        {
            (int result, List<string> lines) =
                            Launcher.Start(
                                workingDir: Directory.GetCurrentDirectory(),
                                fileName: "test.exe",
                                arguments: "fail",
                                redirectStandardOutput: true);
            Assert.AreEqual(-100, result);
            Assert.AreEqual(5, lines.Count);
            Assert.IsTrue(lines.Contains("fail"));
            Assert.IsTrue(lines.Contains("error"));
            Assert.IsTrue(lines.Contains("rejected"));
        }

        [TestMethod()]
        public void LaunchInThreadTest()
        {
            int result =
                       Launcher.LaunchInThread(
                           workingDir: Directory.GetCurrentDirectory(),
                           fileName: "test.exe",
                           arguments: "pass"
                           );
            Assert.AreEqual(0, result);
        }

        [TestMethod()]
        public void ProcessStartTestWithLauncherParameters()
        {
            Parameters launcherParameters = new Parameters
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