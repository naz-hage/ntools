using Ntools;
using System;
using System.Collections.Generic;

namespace NbuildTasks
{
    public class FileMappins
    {
        // This list is used to get the directory location to be used in the FileName of process.StartInfo
        private static readonly List<string> FileMappings = new List<string>()
            {
                "powershell.exe",
                "msiexec.exe",
                "xcopy.exe",
                "robocopy.exe",
                "reg.exe"
            };

        public static string GetFullPathOfFile(string fileName)
        {
            foreach (var mapping in FileMappings)
            {
                if (fileName.Equals(mapping, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{ShellUtility.GetFullPathOfFile(mapping)}";
                }
            }

            return fileName;
        }
    }
}
