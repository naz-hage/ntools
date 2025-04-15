using System.Diagnostics;

namespace Ngit;

class Program
{
    static int Main(string[] args)
    {
        // Redirect to nb.exe with the "git" subcommand
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "nb",
                Arguments = "git " + string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        return process.ExitCode;
    }
}
