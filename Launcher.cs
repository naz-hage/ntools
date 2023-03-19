using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Launcher
{
    public static class Launcher
    {
        public static bool Verbose { get; set; } = false;

        /// <summary>
        /// A wrapper to launch an executable with arguments
        /// an executable must run silent and not require any user input.
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="fileName">The file name to launch</param>
        /// <param name="arguments">The command line arguments</param>
        /// <param name="redirectStandardOutput">Show the console output</param>
        /// <param name="verbose">Show additional output.  messages are preceded by launcher=></param>
        /// <param name="useShellExecute"></param>
        /// <returns>0 if successful; otherwise non-zero.  message is possible if executable has standard output</returns>
        private static ResultHelperEx Start(string workingDir,
                    string fileName,
                    string arguments,
                    bool redirectStandardOutput = false,
                    bool verbose = false,
                    bool useShellExecute = false)
        {
            var result = ResultHelperEx.New();
            Verbose = verbose;

            if (Verbose) Console.WriteLine($"launcher=>{fileName} {arguments}");

            try
            {
                using var process = new Process();
                process.StartInfo.WorkingDirectory = workingDir;
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = useShellExecute;
                process.StartInfo.RedirectStandardOutput = redirectStandardOutput;
                process.StartInfo.RedirectStandardError = redirectStandardOutput;

                Directory.SetCurrentDirectory(process.StartInfo.WorkingDirectory);

                if (process.Start())
                {
                    if (!process.StartInfo.RedirectStandardOutput)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            var line = process?.StandardOutput.ReadLine();
                            WriteError(line);
                            result.Output.Add(line);
                        }
                        while (!process.StandardError.EndOfStream)
                        {
                            var line = process?.StandardError.ReadLine();
                            WriteError(line);
                            result.Output.Add(line);
                        }
                    }
                }
                if (Verbose) Console.WriteLine($"Exit Code: {process.ExitCode}");
                result.Code = process.ExitCode;
                return result;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"ProcessStart Exception: {ex.Message}");
                result.Output.Add($"ProcessStart Exception: {ex.Message}");
                result.Code = int.MaxValue;
                return result;
            }
        }
        /// <summary>
        /// Launch a process specified in parameters 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ResultHelperEx Start(Parameters parameters)
        {

            var result = Start(
                                parameters.WorkingDir,
                                parameters.FileName,
                                parameters.Arguments,
                                parameters.RedirectStandardOutput,
                                parameters.Verbose,
                                parameters.UseShellExecute
                                );
            return result;
        }

        private static void WriteError(string line)
        {
            if (line.ToLower().Contains("error") ||
                line.ToLower().Contains("fail") ||
                line.ToLower().Contains("rejected"))
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Launch in thread and exit
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="workingDir"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static ResultHelperEx LaunchInThread(string workingDir, string fileName, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDir,
                Arguments = arguments,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                CreateNoWindow = true,
            };

            try
            {
                var executable = $"{workingDir}\\{fileName}";
                if (!File.Exists(executable))
                {
                    return ResultHelperEx.Fail(message: $"File {executable} not found");
                }
                new Thread(() =>
                {
                    try
                    {
                        Thread.CurrentThread.IsBackground = true;
                        Process.Start(startInfo);
                        Console.WriteLine($"Started {startInfo.FileName} {startInfo.Arguments}");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }).Start();

                return ResultHelperEx.Success();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return ResultHelperEx.Fail(message:ex.Message);
            }
        }
    }
}
