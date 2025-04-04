using System;
using System.Diagnostics;
using System.Reflection;

namespace NbuildTasks
{
    public static class Nversion
    {
        public const string ExecutingAssembly = "executing";

        /// <summary>
        /// Retrieves the file version information for the entry or executing assembly.
        /// </summary>
        /// <param name="assembly">The assembly to retrieve the file version information from. If empty, retrieves information from the entry assembly.</param>
        /// <returns>The formatted string containing the file description, product name, company name, legal copyright, and file version.</returns>
        public static string Get(string assembly = "")
        {

            FileVersionInfo fileVersionInfo = FileVersionInfo
                                                .GetVersionInfo(Assembly
                                                .GetEntryAssembly()
                                                .Location);
            if (assembly == ExecutingAssembly)
            {
                fileVersionInfo = FileVersionInfo.
                                                GetVersionInfo(Assembly.
                                                GetExecutingAssembly().
                                                Location);

            }

            string updatedCopyright = fileVersionInfo
                            .LegalCopyright
                            .Replace("XXXX", DateTime.Now.Year.ToString());

            return $" *** {fileVersionInfo.FileDescription}, " +
                            $"{fileVersionInfo.ProductName}, " +
                            $"{fileVersionInfo.CompanyName}, " +
                            $"{updatedCopyright} - " +
                            $" Version: {fileVersionInfo.FileVersion}";

        }
    }
}
