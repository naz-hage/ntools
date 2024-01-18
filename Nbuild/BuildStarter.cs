using Launcher;
using NbuildTasks;
using OutputColorizer;
using System.Text;
using System.Xml;

namespace Nbuild;

public class BuildStarter
{
    public static string LogFile { get; set; } = "nbuild.log";
    private const string BuildFileName = "nbuild.targets";
    private const string CommonBuildFileName = "common.targets";
    private const string NbuildBatchFile = "Nbuild.bat";
    private const string ResourceLocation = "Nbuild.resources.nbuild.bat";
    private const string TargetsMd = "targets.md";


    public static ResultHelper Build(string? target, bool verbose)
    {
        string nbuildPath = Path.Combine(Environment.CurrentDirectory, BuildFileName);
        string commonBuildXmlPath = Path.Combine($"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild", CommonBuildFileName);

        if (!File.Exists(nbuildPath))
        {
            return ResultHelper.Fail(-1, $"'{nbuildPath}' not found.");
        }

        // check if target is valid
        if (!ValidTarget(target))
        {
            return ResultHelper.Fail(-1, $"Target '{target}' not found");
        }

        ExtractBatchFile();

        Console.WriteLine($"MSBuild started with '{target ?? "Default"}' target");

        string cmd;

        if (string.IsNullOrEmpty(target))
        {
            cmd = $"{nbuildPath} -fl -flp:logfile={LogFile};verbosity=normal";
        }
        else
        {
            cmd = $"{nbuildPath} /t:{target} -fl -flp:logfile={LogFile};verbosity=normal";
        }

        var parameters = new Parameters
        {
            WorkingDir = Directory.GetCurrentDirectory(),
            FileName = "msbuild.exe",
            Arguments = cmd,
            Verbose = true,
            RedirectStandardOutput = false,
        };

        Console.WriteLine($"==> {parameters.FileName} {parameters.Arguments}");

        var result = Launcher.Launcher.Start(parameters);

        DisplayLog(5);
        return result;
    }

    public static bool ValidTarget(string targetsFile, string? target)
    {
        return GetTargets(targetsFile).Contains(target, StringComparer.OrdinalIgnoreCase);
    }

    public static bool ValidTarget(string? target)
    {
        // check if target is valid in the current directory
        string nbuildPath = Path.Combine(Environment.CurrentDirectory, BuildFileName);
        if (ValidTarget(nbuildPath, target)) return true;

        // check if target is valid in the target files in $(ProgramFiles)\nbuild

        List<string> TargetFiles =
        [
            "common.targets",
            "git.targets",
            "dotnet.targets",
            "code.targets",
            "node.targets",
            "nuget.targets",
            "ngit.targets",
            "mongodb.targets",
        ];

        bool found = false;
        foreach (var targetFile in TargetFiles)
        {
            var path = Path.Combine($"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild", targetFile);
            if (ValidTarget(path, target))
            {
                found = true;
                break;
            }
        }

        return found;
    }

    private static void ExtractBatchFile()
    {
        // Always extract nbuild.bat common.targets.xml
        ResourceHelper.ExtractEmbeddedResourceFromCallingAssembly(ResourceLocation, Path.Combine(Environment.CurrentDirectory, NbuildBatchFile));
        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!Extracted '{NbuildBatchFile}' to {Environment.CurrentDirectory}]\n");
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

    public static IEnumerable<string> GetTargetsAndComments(string targetFileName)
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, targetFileName);
        string[] lines = File.ReadAllLines(filePath);

        StringBuilder commentBuilder = new();
        bool isComment = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("<!--") && trimmedLine.EndsWith("-->"))
            {
                commentBuilder.Clear(); // reset the comment
                // Single-line comment
                commentBuilder.Append(trimmedLine.Substring(4, trimmedLine.Length - 7).Trim());
                isComment = false;
            }
            else if (trimmedLine.StartsWith("<!--"))
            {
                commentBuilder.Clear(); // reset the comment
                isComment = true;
                commentBuilder.Append(trimmedLine.Substring(4).Trim());
            }
            else if (trimmedLine.EndsWith("-->"))
            {
                isComment = false;
                commentBuilder.Append(trimmedLine.Substring(0, trimmedLine.Length - 3).Trim());
            }
            else if (isComment)
            {
                commentBuilder.Append(" " + trimmedLine);
            }
            else if (trimmedLine.StartsWith("<Target "))
            {
                var targetName = line.Trim().Split(' ')[1].Split('=')[1].Trim('"');
                //Console.WriteLine($"{targetName, -20}: {commentBuilder.ToString().Trim()}");
                yield return $"{targetName,-19} | {commentBuilder.ToString().Trim()}";
                commentBuilder.Clear(); // reset the comment
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

    //public static ResultHelper DisplayTargets(string directoryPath)
    //{
    //    // replace $(BuildTools) with environment variable ProgramFiles/Nbuild
    //    directoryPath = directoryPath.Replace("$(BuildTools)", $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild");
    //    string[] targetsFiles = Directory.GetFiles(directoryPath, "*.targets", SearchOption.TopDirectoryOnly);
    //    try
    //    {
    //        foreach (var targetsFile in targetsFiles)
    //        {
    //            Console.WriteLine($"{targetsFile} Targets:");
    //            Console.WriteLine($"----------------------");
    //            foreach (var targetName in GetTargetsAndComments(Path.Combine(directoryPath, targetsFile)))
    //            {
    //                Console.WriteLine(targetName);
    //            }
    //            Console.WriteLine();

    //            Console.WriteLine($"Imported Targets:");
    //            Console.WriteLine($"----------------------");
    //            foreach (var item in GetImportAttributes(targetsFile, "Project"))
    //            {
    //                // replace $(ProgramFiles) with environment variable
    //                var importItem = item.Replace("$(ProgramFiles)", Environment.GetEnvironmentVariable("ProgramFiles"));
    //                importItem = importItem.Replace("$(BuildTools)", $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild");
    //                Console.WriteLine($"{importItem} Targets:");
    //                Console.WriteLine($"----------------------");
    //                foreach (var targetName in GetTargetsAndComments(importItem))
    //                {
    //                    Console.WriteLine(targetName);
    //                }
    //                Console.WriteLine();

    //                // Recursive call for each imported project directory
    //                if (importItem != null)
    //                {
    //                    var importItemDirectory = Path.GetDirectoryName(importItem);
    //                    if (importItemDirectory != null) DisplayTargets(importItemDirectory);
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return ResultHelper.Fail(-1, $"Exception occurred: {ex.Message}");
    //    }

    //    return ResultHelper.Success();
    //}

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

    public static ResultHelper DisplayTargetsInFile(string filePath)
    {
        //replace $(BuildTools) with environment variable ProgramFiles/Nbuild
        filePath = filePath.Replace("$(BuildTools)", $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild");
        try
        {
            using (StreamWriter writer = new(TargetsMd, true))
            {
                Console.WriteLine($"{filePath} Targets:");
                writer.WriteLine($"- **{filePath} Targets**\n");
                Console.WriteLine($"----------------------");
                writer.WriteLine("| **Target Name** | **Description** |");
                writer.WriteLine("| --- | --- |");

                foreach (var targetAndDescription in GetTargetsAndComments(filePath))
                {
                    Console.WriteLine(targetAndDescription);

                    writer.WriteLine($"| {targetAndDescription} |");
                }
                Console.WriteLine();
                writer.WriteLine("\n");

            }
            var importItems = GetImportAttributes(filePath, "Project");
            foreach (var item in importItems)
            {
                // replace $(ProgramFiles) with environment variable
                var importItem = item.Replace("$(ProgramFiles)", Environment.GetEnvironmentVariable("ProgramFiles"));
                importItem = importItem.Replace("$(BuildTools)", $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild");

                using (StreamWriter writer = new(TargetsMd, true))
                {
                    Console.WriteLine($"Imported Targets:");
                    //writer.WriteLine($"- Imported Targets: {importItem}\n");
                    Console.WriteLine($"----------------------");
                }
                // Recursive call for each imported target file
                DisplayTargetsInFile(importItem);
            }

        }
        catch (Exception ex)
        {
            return ResultHelper.Fail(-1, $"Exception occurred: {ex.Message}");
        }

        return ResultHelper.Success();
    }

    public static ResultHelper DisplayTargets(string directoryPath)
    {
        if (File.Exists(TargetsMd))
        {
            File.Delete(TargetsMd);
        }

        string[] targetsFiles = Directory.GetFiles(directoryPath, "*.targets", SearchOption.TopDirectoryOnly);
        foreach (var targetsFile in targetsFiles)
        {
            DisplayTargetsInFile(targetsFile);
        }
        return ResultHelper.Success();
    }
}

