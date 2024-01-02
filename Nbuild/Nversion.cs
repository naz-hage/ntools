using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace nbuild
{
    public class Nversion
    {
        public static string Get()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.
                                                GetVersionInfo(Assembly.
                                                GetExecutingAssembly().
                                                Location);

            return new StringBuilder()
                            .Append($" *** {fileVersionInfo.FileDescription}, ")
                            .Append($"{fileVersionInfo.ProductName}, ")
                            .Append($"{fileVersionInfo.CompanyName}, ")
                            .Append($"{fileVersionInfo.LegalCopyright} ")
                            .Append($"Version: {fileVersionInfo.FileVersion}")
                            .ToString();
        }
    }
}
