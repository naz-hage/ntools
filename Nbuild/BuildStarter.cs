using NbuildTasks;
using Ntools;
using OutputColorizer;
using System.Diagnostics;
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
    private const string MsbuildExe = "msbuild.exe";

    /// <summary>
    /// Builds the specified target using nbuild.
    /// </summary>
    /// <param name="target">The target to build. If null, the default target will be built.</param>
    /// <param name="verbose">Specifies whether to display verbose output during the build process.</param>
    /// <returns>A <see cref="ResultHelper"/> object representing the result of the build operation.</returns>
    public static ResultHelper Build(string? target, bool verbose = false)
    {
        string nbuildPath = Path.Combine(Environment.CurrentDirectory, BuildFileName);
        string commonBuildXmlPath = Path.Combine($"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild", CommonBuildFileName);

        if (!File.Exists(nbuildPath))
        {
            return ResultHelper.Fail(-1, $"'{nbuildPath}' not found.");
        }

        // check if target is valid
        if (!ValidTarget(target, verbose))
        {
            return ResultHelper.Fail(-1, $"Target '{target}' not found");
        }

        ExtractBatchFile();

        Console.WriteLine($"MSBuild started with Target: '{target ?? "Default"}' | verbose:{ verbose}");

        LogFile = Path.Combine(Environment.CurrentDirectory, LogFile);
        string cmd = string.IsNullOrEmpty(target)
            ? $"{nbuildPath} -fl -flp:logfile={LogFile};verbosity=normal"
            : $"{nbuildPath} /t:{target} -p:TargetName={target} -fl -flp:logfile={LogFile};verbosity=normal";

        if (verbose)
        {
            Console.WriteLine($"==> {cmd}");
        }

        //  Get location of msbuild.exe
        //var msbuildPath = ShellUtility.GetFullPathOfFile(MsbuildExe);
        var msbuildPath = FindMsBuild64BitPath(verbose: true);

        if (verbose)
        {
            Console.WriteLine($"MSBuild Path: {msbuildPath}");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(msbuildPath),
                FileName = MsbuildExe,
                Arguments = cmd,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = false,
            }
        };

        Console.WriteLine($"==> {process.StartInfo.FileName} {process.StartInfo.Arguments}");

        var result = process.LockStart(verbose);

        DisplayLog(5);
        return result;
    }

    // <summary>
    /// Checks if the specified target is valid in the given targets file.
    /// </summary>
    /// <param name="targetsFile">The path to the targets file.</param>
    /// <param name="target">The target to check.</param>
    /// <returns>True if the target is valid, false otherwise.</returns>
    public static bool ValidTarget(string targetsFile, string? target)
    {
        return GetTargets(targetsFile).Contains(target, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the specified target is valid in the given targets file.
    /// </summary>
    /// <param name="target">The target to check.</param>
    /// <returns>True if the target is valid, false otherwise.</returns>
    public static bool ValidTarget(string? target, bool verbose)
    {
        // check if target is valid in the current directory
        string nbuildPath = Path.Combine(Environment.CurrentDirectory, BuildFileName);
        if (ValidTarget(nbuildPath, target)) return true;

        List<string> KnownTargetFiles = new List<string>();

        // Find list of *.targets files in ntools deployment folder
        string[] targetFiles = Directory.GetFiles($"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild", "*.targets");
        if (targetFiles == null)
        {
            return false;
        }

        // Add target files to TargetFiles list
        KnownTargetFiles.AddRange(targetFiles);
        
        // Find list of *.targets files in current folder
        targetFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.targets");

        // Add target files to TargetFiles list
        KnownTargetFiles.AddRange(targetFiles);

        foreach (var file in targetFiles)
        {
            var importTargets = BuildStarter.GetImportAttributes(file, "Project");

            KnownTargetFiles.AddRange(importTargets);
        }

        if (verbose)
        {
            foreach (var file in KnownTargetFiles)
            {
                Console.WriteLine(file);
            }
        }

        bool found = false;
        foreach (var targetFile in KnownTargetFiles)
        {
            if (ValidTarget(targetFile, target))
            {
                found = true;
                break;
            }
        }

        return found;
    }
    
    ///<summary>
    /// Extracts the batch file from the embedded resource.
    /// </summary>
    private static void ExtractBatchFile()
    {
        // Always extract nbuild.bat common.targets.xml
        ResourceHelper.ExtractEmbeddedResourceFromCallingAssembly(ResourceLocation, Path.Combine(Environment.CurrentDirectory, NbuildBatchFile));
        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!Extracted '{NbuildBatchFile}' to {Environment.CurrentDirectory}]\n");
    }

    /// <summary>
    /// Retrieves the targets from the specified XML document.
    /// </summary>
    /// <param name="targetsFile">The path to the XML document.</param>
    /// <returns>An enumerable collection of target names.</returns>
    public static IEnumerable<string> GetTargets(string targetsFile)
    {
        var buildXmlFile = Path.Combine(Environment.CurrentDirectory, targetsFile);
        if (!File.Exists(buildXmlFile))
        {
            throw new FileNotFoundException($"'{targetsFile}' file not found.", buildXmlFile);
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(buildXmlFile);

        XmlNodeList targets = doc.GetElementsByTagName("Target");

        if (targets != null)
        {
            foreach (XmlNode target in targets)
            {
                if (target.Attributes != null && target.Attributes["Name"] != null)
                {
                    var attributeName = target?.Attributes?["Name"]?.Value;
                    if (!string.IsNullOrEmpty(attributeName))
                    {
                        yield return attributeName;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Parses the specified file and returns a sequence of strings where each string contains a target name and its associated comment.
    /// </summary>
    /// <param name="targetFileName">The name of the file to parse. The file should be in the current directory.</param>
    /// <returns>A sequence of strings where each string is in the format "TargetName | Comment".</returns>
    public static IEnumerable<string> GetTargetsAndComments(string targetFileName)
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, targetFileName);
        string[] lines = File.ReadAllLines(filePath);

        StringBuilder commentBuilder = new();
        bool isComment = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Single-line comment
            if (trimmedLine.StartsWith("<!--") && trimmedLine.EndsWith("-->"))
            {
                commentBuilder.Clear(); // reset the comment
                // Single-line comment
                commentBuilder.Append(trimmedLine.Substring(4, trimmedLine.Length - 7).Trim());
                isComment = false;
            }
            // start of multi-line comment
            else if (trimmedLine.StartsWith("<!--"))
            {
                commentBuilder.Clear(); // reset the comment
                isComment = true;
                commentBuilder.Append(trimmedLine.Substring(4).Trim());
            }
            // End of multi-line comment
            else if (trimmedLine.EndsWith("-->"))
            {
                isComment = false;
                commentBuilder.Append(" " + trimmedLine.Substring(0, trimmedLine.Length - 3).Trim());
            }
            // Multi-line comment
            else if (isComment)
            {
                commentBuilder.Append(" " + trimmedLine);
            }
            // Target line, extract target name
            else if (trimmedLine.StartsWith("<Target "))
            {
                string targetName = string.Empty;
                string[] parts = line.Trim().Trim('>').Trim('"').Split(' ');
                if (parts.Length > 1)
                {
                    string[] subParts = parts[1].Split('=');
                    if (subParts.Length > 1)
                    {
                        targetName = subParts[1].Trim(' ');
                        targetName = subParts[1].Trim('>');
                        targetName = subParts[1].Trim('"');
                        // Rest of your code that uses targetName...
                    }
                }

                yield return $"{targetName,-19} | {commentBuilder.ToString().Trim()}";
                commentBuilder.Clear(); // reset the comment
            }
        }
    }

    /// <summary>
    /// Displays the log file content.
    /// </summary>
    /// <param name="lastLines">The number of last lines to display.</param>
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

    /// <summary>
    /// Reads the common.targets file and returns the specified attributes, with optional replacements for placeholders.
    /// </summary>
    /// <param name="filePath">The path to the common.targets file.</param>
    /// <param name="attributeName">The name of the attribute to retrieve.</param>
    /// <param name="replacements">A dictionary of placeholders and their replacements.</param>
    /// <returns>An enumerable collection of modified attributes.</returns>
    //public static IEnumerable<string> GetImportAttributes(string filePath, string attributeName, Dictionary<string, string>? replacements = null)
    public static IEnumerable<string> GetImportAttributes(string filePath, string attributeName)
    {
        var ntoolsEnv = new NtoolsEnvironmentVariables();
        var replacements = new Dictionary<string, string>
                    {
                        { "$(MainDir)", ntoolsEnv.MainDir },
                        { "$(DevDrive)", ntoolsEnv.DevDrive },
                        { "$(ProgramFiles)", Environment.GetEnvironmentVariable("ProgramFiles") ?? string.Empty },
                        { "$(BuildTools)", $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\nbuild" }
                    };

        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);

        XmlNodeList imports = doc.GetElementsByTagName("Import");

        foreach (XmlNode import in imports)
        {
            if (import.Attributes != null && import.Attributes[attributeName] != null)
            {
                string attributeValue = import.Attributes[attributeName]?.Value ?? string.Empty;

                if (replacements != null)
                {
                    foreach (var replacement in replacements)
                    {
                        attributeValue = attributeValue.Replace(replacement.Key, replacement.Value);
                    }
                }

                yield return attributeValue;
            }
        }
    }

    /// <summary>
    /// Retrieves the path to the 64-bit MSBuild executable.
    /// </summary>
    /// <returns>The path to the 64-bit MSBuild executable, or null if not found.</returns>
    public static string? FindMsBuild64BitPath(bool verbose = false)
    {
        // Define the directories where msbuild.exe might be located
        var possibleDirectories = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio\\2022\\Professional\\MSBuild\\Current\\Bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio\\2022\\Enterprise\\MSBuild\\Current\\Bin"),
        };

        if (verbose)
        {
            Console.WriteLine("Searching for msbuild.exe in the following directories:");
            foreach (var dir in possibleDirectories)
            {
                Console.WriteLine(dir);
            }
        }

        // Search for msbuild.exe in the directories and their subdirectories
        var msbuildPaths = possibleDirectories
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "msbuild.exe", SearchOption.AllDirectories))
            .Where(path => !path.Contains("Preview") && path.Contains("amd64"))
            .ToList();

        if (verbose)
        {
            Console.WriteLine("Found the following msbuild.exe paths:");
            foreach (var path in msbuildPaths)
            {
                Console.WriteLine(path);
            }
        }

        // Return the first path found, or null if no path was found
        return msbuildPaths.FirstOrDefault();
    }

    /// <summary>
    /// Displays the targets in the specified file.
    /// </summary>
    /// <param name="filePath">The path to the target file.</param>
    /// <returns>A <see cref="ResultHelper"/> object representing the result of the operation.</returns>
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

                    var targets = GetTargetsAndComments(filePath).ToList();
                    if (targets.Count > 0)
                    {
                        foreach (var targetAndDescription in targets)
                        {
                            Console.WriteLine(targetAndDescription);
                            writer.WriteLine($"| {targetAndDescription} |");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No targets found.");
                    }
                    Console.WriteLine();
                    writer.WriteLine("\n");

                }
                var importItems = GetImportAttributes(filePath, "Project");
                foreach (var item in importItems)
                {
                    using (StreamWriter writer = new(TargetsMd, true))
                    {
                        Console.WriteLine($"Imported Targets:");
                        //writer.WriteLine($"- Imported Targets: {importItem}\n");
                        Console.WriteLine($"----------------------");
                    }
                    // Recursive call for each imported target file
                    DisplayTargetsInFile(item);
                }

            }
            catch (Exception ex)
            {
                return ResultHelper.Fail(-1, $"Exception occurred: {ex.Message}");
            }

            return ResultHelper.Success();
        }

    /// <summary>
    /// Displays the targets in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory containing the target files.</param>
    /// <returns>A <see cref="ResultHelper"/> object representing the result of the operation.</returns>
    public static ResultHelper DisplayTargets(string directoryPath)
    {
        if (File.Exists(TargetsMd))
        {
            File.Delete(TargetsMd);
        }

        string[] targetsFiles = Directory.GetFiles(directoryPath, "*.targets", SearchOption.TopDirectoryOnly);
        foreach (var targetsFile in targetsFiles)
        {
            var result = DisplayTargetsInFile(targetsFile);
            if (!result.IsSuccess())
            {
                return result;
            }
        }
        return ResultHelper.Success();
    }
}

