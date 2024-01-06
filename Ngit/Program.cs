// See https://aka.ms/new-console-template for more information

using CommandLine;
using NbuildTasks;
using Ngit;
using OutputColorizer;
using System.Diagnostics;
using static Ngit.Enums;


var watch = Stopwatch.StartNew();
RetCode ReturnCode = RetCode.InvalidParameter;

if (!Parser.TryParse(args, out Cli options))
{
    if (!args[0].Equals("--help", StringComparison.CurrentCultureIgnoreCase))
        Console.WriteLine($"Ngit completed with '-1'");
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

watch.Stop();

if (ReturnCode != RetCode.Success)
{
    Colorizer.WriteLine($"Ngit completed in {watch.ElapsedMilliseconds} ms with [{ConsoleColor.Red}!'{ReturnCode}']");
}

return Convert.ToInt32(ReturnCode);
