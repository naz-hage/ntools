using System;
using System.IO;
using System.Linq;

namespace NbuildTasks
{
    /// <summary>
    /// Represents the environment variables for Ntools.
    /// </summary>
    public class NtoolsEnvironmentVariables
    {
        /// <summary>
        /// Gets or sets the development drive.
        /// </summary>
        public string DevDrive { get; set; }

        /// <summary>
        /// Gets or sets the main directory.
        /// </summary>
        public string MainDir { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtoolsEnvironmentVariables"/> class.
        /// </summary>
        /// <param name="testMode">Indicates whether the instance is created in test mode.</param>
        public NtoolsEnvironmentVariables(bool testMode = false)
        {
            DevDrive = GetDevDrive();

            MainDir = GetMainDir(testMode);
        }

        /// <summary>
        /// Gets the development drive.
        /// </summary>
        /// <returns>The development drive path.</returns>
        public static string GetDevDrive()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Path.GetPathRoot(currentDirectory);
        }

        /// <summary>
        /// Gets the main directory.
        /// </summary>
        /// <param name="testMode">Indicates whether the test mode is enabled.</param>
        /// <returns>The main directory path.</returns>
        public static string GetMainDir(bool testMode = false)
        {
            var parentDir = Directory.GetParent(Environment.CurrentDirectory).FullName;
            var mainDir = Path.GetFullPath(parentDir).Substring(Path.GetPathRoot(parentDir).Length);
            if (testMode)
            {
                mainDir = mainDir.EndsWith("ntools") ? mainDir.Substring(0, mainDir.Length - 7) : mainDir;
            }

            return mainDir;
        }
    }
}