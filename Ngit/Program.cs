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

if (!string.IsNullOrEmpty(options.GitCommand))
{
    if (options.Verbose)
    {
        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
    }
    ReturnCode = Command.Process(options);
}

if (ReturnCode != RetCode.Success)
{

    Colorizer.WriteLine($"{NgitAssemblyExe} Completed with [{ConsoleColor.Red}!'{ReturnCode}']");
}

return Convert.ToInt32(ReturnCode);
