using Launcher;
using OutputColorizer;
using System.Diagnostics;
using System.Xml;

namespace nbuild
{
    public class BuildStarter
    {
        public static string LogFile { get; set; } = "nbuild.log";
        public const string BuildFileName = "nbuild.targets";
        public const string CommonBuildFileName = "common.targets";
        private const string NbuildBatchFile = "nbuild.bat";


        public static ResultHelper Build(string? target, bool verbose)
        {
            string buildXmlPath = Path.Combine(Environment.CurrentDirectory, BuildFileName);
            string commonBuildXmlPath = Path.Combine($"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild", CommonBuildFileName);

      
            // Always extract nbuild.bat common.targets.xml
            //ResourceHelper.ExtractEmbeddedResource(Environment.CurrentDirectory, "nbuild.resources.common.targets", CommonBuildFileName);
            ResourceHelper.ExtractEmbeddedResource(Environment.CurrentDirectory, "nbuild.resources.nbuild.bat", NbuildBatchFile);
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!Extracted {NbuildBatchFile} & {CommonBuildFileName} to {Environment.CurrentDirectory}]\n");

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
            DisplayTag();
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

        private static void DisplayLog(int lastLines)
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

        private static void DisplayTag()
        {
            //print tag.txt to console
            string tagFilePath = Path.Combine(Environment.CurrentDirectory, "tag.txt");
            if (File.Exists(tagFilePath))
            {
                string tag = File.ReadAllText(tagFilePath);
                Console.WriteLine($"Tag: {tag}");
            }
        }

    }
}

