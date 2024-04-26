using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.IO.Compression;

namespace NbuildTasks
{

    public class UnZip : Task
    {
        [Required]
        public string FileName { get; set; }

        [Required]
        public string Destination { get; set; }
        public override bool Execute()
        {
            try
            {
                if (!File.Exists(FileName))
                {
                    Log.LogError($"File {FileName} does not exist");
                    return false;
                }

                // Delete the directory if it exists.
                if (Directory.Exists(Destination))
                {
                    Directory.Delete(Destination, true);
                }

                // Create the directory.
                Directory.CreateDirectory(Destination);

                ZipFile.ExtractToDirectory(FileName, Destination);

                Log.LogMessage($"Unzipped {FileName} to {Destination}");

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to unzip file: {ex.Message}");
                return false;
            }
        }
    }
}
