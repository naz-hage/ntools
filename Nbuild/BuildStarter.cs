using Launcher;
using NbuildTasks;
using OutputColorizer;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Nbuild
{
    public class BuildStarter
    {
        public static string LogFile { get; set; } = "nbuild.log";
        public const string BuildFileName = "nbuild.targets";
        public const string CommonBuildFileName = "common.targets";
        private const string NbuildBatchFile = "Nbuild.bat";
        private const string ResourceLocation = "Nbuild.resources.nbuild.bat";

        public static ResultHelper Build(string? target, bool verbose)
        {
            string buildXmlPath = Path.Combine(Environment.CurrentDirectory, BuildFileName);
            string commonBuildXmlPath = Path.Combine($"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild", CommonBuildFileName);

      
            // Always extract nbuild.bat common.targets.xml
            ResourceHelper.ExtractEmbeddedResourceFromCallingAssembly(ResourceLocation, Path.Combine(Environment.CurrentDirectory, NbuildBatchFile));
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!Extracted '{NbuildBatchFile}' to {Environment.CurrentDirectory}]\n");

            if (!File.Exists(buildXmlPath))
            {
                return ResultHelper.Fail(-1, $"'{buildXmlPath}' not found.");
            }

            // check if target is valid
            if (!string.IsNullOrEmpty(target) &&
                !BuildStarter.GetTargets(buildXmlPath).Contains(target, StringComparer.OrdinalIgnoreCase) &&
                !BuildStarter.GetTargets(commonBuildXmlPath).Contains(target, StringComparer.OrdinalIgnoreCase))
                return ResultHelper.Fail(-11, $"Target '{target}' not found in {buildXmlPath} or {commonBuildXmlPath}");

            Console.WriteLine($"MSBuild started with '{target ?? "Default"}' target");

            return LaunchBuild(buildXmlPath, target, verbose);
        }

        private static ResultHelper LaunchBuild(string buildFile, string? target, bool verbose = true)
        {
            string cmd;

            if (string.IsNullOrEmpty(target))
            {
                cmd = $"{buildFile} -fl -flp:logfile={LogFile};verbosity=normal";
            }
            else
            {
                cmd = $"{buildFile} /t:{target} -fl -flp:logfile={LogFile};verbosity=normal";
            }

            var parameters = new Parameters
            {
                WorkingDir = Directory.GetCurrentDirectory(),
                FileName = "msbuild.exe",
                Arguments = cmd,
                Verbose = verbose,
                RedirectStandardOutput = !verbose,
            };

            Console.WriteLine($"==> {parameters.FileName} {parameters.Arguments}");

            var result = Launcher.Launcher.Start(parameters);

            DisplayLog(5);
            return result;
        }

        public static IEnumerable<string> GetTargets(string targetsFile)
        {
            var buildXmllFile = Path.Combine(Environment.CurrentDirectory, targetsFile);
            if (!File.Exists(targetsFile))
            {
                throw new FileNotFoundException($"'{targetsFile}' file not found.", buildXmllFile);
            }

            XmlDocument doc = new();
            doc.Load(targetsFile);

            XmlNodeList? targets = doc.GetElementsByTagName("Target");

            if (targets != null)
            {
                foreach (XmlNode? target in targets)
                {
                    if (target != null && target.Attributes != null && target.Attributes["Name"] != null)
                    {
                        var attributeName = target?.Attributes?["Name"]?.Value;
                        if (attributeName != null)
                        {
                            yield return attributeName;
                        }
                    }
                }
            }
        }

        public static void DisplayLog(int lastLines)
        {
            string logFilePath = Path.Combine(Environment.CurrentDirectory, LogFile);
            if (File.Exists(logFilePath))
            {
                string[] lines = File.ReadAllLines(logFilePath);
                int start = lines.Length - lastLines;
                if (start < 0)
                {
                    start = 0;
                }
                for (int i = start; i < lines.Length; i++)
                {
                    Console.WriteLine(lines[i]);
                }
            }
        }

        public static ResultHelper DisplayTargets()
        {
            string[] targetsFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.targets", SearchOption.TopDirectoryOnly);
            try
            {
                foreach (var targetsFile in targetsFiles)
                {
                    Console.WriteLine($"{targetsFile} Targets:");
                    Console.WriteLine($"----------------------");
                    foreach (var targetName in BuildStarter.GetTargets(Path.Combine(Environment.CurrentDirectory, targetsFile)))
                    {
                        Console.WriteLine(targetName);
                    }
                    Console.WriteLine();

                    Console.WriteLine($"Imported Targets:");
                    Console.WriteLine($"----------------------");
                    foreach (var item in BuildStarter.GetImportAttributes(targetsFile, "Project"))
                    {
                        // replace $(ProgramFiles) with environment variable
                        var importItem = item.Replace("$(ProgramFiles)", Environment.GetEnvironmentVariable("ProgramFiles"));
                        Console.WriteLine($"{importItem} Targets:");
                        Console.WriteLine($"----------------------");
                        foreach (var targetName in BuildStarter.GetTargets(importItem))
                        {
                            Console.WriteLine(targetName);
                        }
                        Console.WriteLine();

                    }
                }
            }
            catch (Exception ex)
            {
                return ResultHelper.Fail( -1, $"Exception occurred: {ex.Message}");
            }

            return ResultHelper.Success();
        }

        public static IEnumerable<string> GetImportAttributes(string filePath, string attributeName)
        {
            XmlDocument doc = new();
            doc.Load(filePath);

            XmlNodeList? imports = doc.GetElementsByTagName("Import");

            if (imports != null)
            {
                foreach (XmlNode? import in imports)
                {
                    if (import != null && import.Attributes != null && import.Attributes[attributeName] != null)
                    {
                        var projectName = import?.Attributes?[attributeName]?.Value;
                        if (projectName != null)
                        {
                            yield return projectName;
                        }
                    }
                }
            }
        }
    }
}

