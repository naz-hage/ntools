using System.Diagnostics;
using System.Reflection;

namespace Nbackup
{
    public class Nversion
    {
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
