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
            var currentDirectory = Environment.CurrentDirectory;
            // Get the Current Drive from the current working directory
            var currentDrive = Environment.CurrentDirectory.Split(':').First();
            DevDrive = $"{currentDrive}:";

            // Get the Main/Parent Directory from the current working directory
            var mainDir = Directory.GetParent(currentDirectory).FullName;

            // Get the Main Directory from the current working directory which is full path minus last directory
            // Strip the drive letter and '\\' from mainDir
            MainDir = Path.GetFullPath(mainDir).Substring(Path.GetPathRoot(mainDir).Length);
            if (testMode)
            {
                MainDir = MainDir.EndsWith("ntools") ? MainDir.Substring(0, MainDir.Length - 7) : MainDir;
            }
        }
    }
}