using launcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Launcher
{
    public static class Launcher
    {
        public static bool Verbose { get; set; } = false;
        /// <summary>
        /// A wrapper to launch and executable with arguments
        /// an executable must run silent and not require any user input.
        /// </summary>
        /// <param name="fileName">The file name to launch</param>
        /// <param name="arguments">The command line arguments</param>
        /// <param name="redirectStandardOutput">Show the console output</param>
        /// <param name="verbose">Show additional output.  messages are precded by launcher=></param>
        /// <returns>0 if successful; otherwise non-zero.  message is possible if executable has standard output</returns>
        public static (int, List<string>) Start(string workingDir, string fileName, string arguments, bool redirectStandardOutput=false, bool verbose = false)
        {
            Verbose = verbose;
            List<string> output = new();
            
            if (Verbose) Console.WriteLine($"launcher=>{fileName} {arguments}");
            
            try
            {
                using Process process = new();
                process.StartInfo.WorkingDirectory = workingDir;
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = false;
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
                            output.Add(line);
                        }
                        while (!process.StandardError.EndOfStream)
                        {
                            var line = process?.StandardError.ReadLine();
                            WriteError(line);
                            output.Add(line);
                        }
                    }
                }
                if (Verbose) Console.WriteLine($"Exit Code: {process.ExitCode}");
                return (process.ExitCode, output);
            }

            catch (Exception ex)
            {
                Console.WriteLine($"ProcessStart Exception: {ex.Message}");
                output.Add($"ProcessStart Exception: {ex.Message}");
                return (int.MaxValue, output);
            }
        }

        /// <summary>
        /// An alternative to 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Result Start(Parameters parameters)
        {
            var result = new Result();

            (result.Code, result.Output) = Start(
                                                parameters.WorkingDir,
                                                parameters.FileName,
                                                parameters.Arguments,
                                                parameters.RedirectStandardOutput,
                                                parameters.Verbose
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
        public static int LaunchInThread(string workingDir, string fileName, string arguments)
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
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Process.Start(startInfo);
                    Console.WriteLine($"Started {startInfo.FileName} {startInfo.Arguments}");
                }).Start();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return 1;
            }
        }
    }
}
