using Ntools;
using System.Collections.Generic;

namespace NbuildTasks
{
    public class FileMappins
    {
        // This dictionary is used to get the directory location to be used in the FileName of process.StartInfo
        private static readonly Dictionary<string, string> FileMappings = new Dictionary<string, string>()
        {
            { "powershell", "powershell.exe" },
            { "msiexec", "msiexec.exe" },
            { "xcopy", "xcopy.exe" },
            { "robocopy", "robocopy.exe" }
        };

        public static string GetFullPathOfFile(string fileName)
        {
            foreach (var mapping in FileMappings)
            {
                if (fileName.Contains(mapping.Key))
                {
                    return $"{ShellUtility.GetFullPathOfFile(mapping.Value)}";
                }
            }

            return fileName;
        }
    }
}
