using System;
using System.IO;

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
            DevDrive = GetDevDrive(testMode);
            MainDir = GetMainDir(testMode);

            SourceDir = $"{DevDrive}\\{MainDir}";
        }

        /// <summary>
        /// Gets the development drive.
        /// </summary>
        /// <param name="testMode">Indicates whether to use test mode paths.</param>
        /// <returns>The development drive path.</returns>
        /// <remarks>
        /// The Development Drive is defined as the root directory of the current working directory.
        /// In test mode, returns the temp directory path.
        /// </remarks>
        private static string GetDevDrive(bool testMode = false)
        {
            if (testMode)
            {
                return Path.GetTempPath().TrimEnd('\\');
            }
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
        /// If test mode is enabled, returns "NbuildTasksTests".
        /// </remarks>
        private static string GetMainDir(bool testMode = false)
        {
            if (testMode)
            {
                return "NbuildTasksTests";
            }
            var parentDir = Directory.GetParent(Environment.CurrentDirectory).FullName;
            var mainDir = Path.GetFullPath(parentDir).Substring(Path.GetPathRoot(parentDir).Length);
            if (mainDir.EndsWith("ntools"))
            {
                mainDir = mainDir.Substring(0, mainDir.Length - 7);
            }
            return mainDir;
        }
    }
}