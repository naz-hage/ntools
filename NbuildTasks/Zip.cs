using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NbuildTasks
{
    public class Zip : Task
    {
        [Required]
        public string Path { get; set; }


        [Required]
        public string FileName { get; set; }

        public override bool Execute()
        {
            try
            {
                if (File.Exists(Path))
                {
                    Log.LogError($"File {Path} does not exist");
                    return false;
                }

                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }

                ZipFile.CreateFromDirectory(Path, FileName);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to zip file: {ex.Message}");
                return false;
            }

        }
    }
}
