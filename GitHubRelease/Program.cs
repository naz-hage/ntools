using System.Diagnostics;

namespace GitHubRelease;

class Program
{
    static int Main(string[] args)
    {
        // Redirect to nb.exe with the "release" subcommand
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "nb",
                Arguments = "release " + string.Join(" ", args),
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
