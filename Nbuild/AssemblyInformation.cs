using System.Diagnostics;
using System.Reflection;

namespace nbuild
{
    public class AssemblyInformation
    {
     
        /// <summary>
        /// return the company, product, version of the executing assembly
        /// </summary>
        /// <returns></returns>
        public static string Get()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.
                                                            GetVersionInfo(Assembly.
                                                                            GetExecutingAssembly().
                                                                            Location);

            return $" *** {fileVersionInfo.FileDescription}, " +
                            $"{fileVersionInfo.ProductName}, " +
                            $"{fileVersionInfo.CompanyName}, " +
                            $"{fileVersionInfo.LegalCopyright} -" +
                            $" Version: {fileVersionInfo.FileVersion}";
        }
    }
}
