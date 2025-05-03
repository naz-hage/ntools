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
        /// Gets the development drive.
        /// </summary>
        public string DevDrive { get; }

        /// <summary>
        /// Gets the main directory.
        /// </summary>
        public string MainDir { get; }

        /// summary>
        /// Gets the Source directory.
        /// </summary>
        public string SourceDir { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtoolsEnvironmentVariables"/> class.
        /// </summary>
        /// <param name="testMode">Indicates whether the instance is created in test mode.</param>
        public NtoolsEnvironmentVariables(bool testMode = false)
        {
            DevDrive = GetDevDrive();

            MainDir = GetMainDir(testMode);

            SourceDir = $"{DevDrive}\\{MainDir}";
        }

        /// <summary>
        /// Gets the development drive.
        /// </summary>
        /// <returns>The development drive path.</returns>
        /// <remarks>
        /// The Development Drive is defined as the root directory of the current working directory.
        /// This method retrieves the root path of the drive where the application is currently running.
        /// </remarks>
        private static string GetDevDrive()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Path.GetPathRoot(currentDirectory);
        }


        /// <summary>
        /// Gets the main directory.
        /// </summary>
        /// <param name="testMode">Indicates whether the test mode is enabled.</param>
        /// <returns>The main directory path.</returns>
        /// <remarks>
        /// The Main Directory is defined as the parent directory of the current working directory 
        /// and calculates the main directory path relative to the root of the drive. 
        /// If test mode is enabled, it ensures that the returned path does not end with "ntools".
        /// </remarks>
        private static string GetMainDir(bool testMode = false)
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