// See https://aka.ms/new-console-template for more information

using CommandLine;
using NbuildTasks;
using Ngit;
using OutputColorizer;
using static NbuildTasks.Enums;


RetCode ReturnCode = RetCode.InvalidParameter;
string NgitAssemblyExe = "Ngit";

if (!Parser.TryParse(args, out Cli options))
{
    if (!args[0].Equals("--help", StringComparison.CurrentCultureIgnoreCase))
        Console.WriteLine($"{NgitAssemblyExe} Completed with '-1'");
    return 0;
}

if (!Enum.IsDefined(options.Command))
{
    if (options.Verbose)
    {
        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
    }
    ReturnCode = Command.Process(options);
}

if (ReturnCode != RetCode.Success)
{
    if (ReturnCode == RetCode.NotAGitRepository)
    {
       if (options.Verbose) Colorizer.WriteLine($"Current directory is not git repo [{ConsoleColor.Cyan}!'{RetCode.NotAGitRepository}']\n");
       ReturnCode = RetCode.Success;
    }
    else
    {
        Colorizer.WriteLine($"{NgitAssemblyExe} Completed with [{ConsoleColor.Red}!'{ReturnCode}']\n");
    }
}

return Convert.ToInt32(ReturnCode);
