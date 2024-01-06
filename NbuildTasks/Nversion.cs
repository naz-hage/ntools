using System.Diagnostics;
using System.Reflection;

namespace NbuildTasks
{
    public class Nversion
    {
        /// <summary>
        /// Retrieves the file version information for the entry assembly.
        /// </summary>
        /// <returns>The formatted string containing the file description, product name, company name, legal copyright, and file version.</returns>
        public static string Get()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo
                                                .GetVersionInfo(Assembly
                                                .GetEntryAssembly()
                                                .Location);

            return $" *** {fileVersionInfo.FileDescription}, " +
                            $"{fileVersionInfo.ProductName}, " +
                            $"{fileVersionInfo.CompanyName}, " +
                            $"{fileVersionInfo.LegalCopyright} -" +
                            $" version: {fileVersionInfo.FileVersion}";
        }
    }
}
