using System;
using System.IO;
using System.Reflection;

namespace NbuildTasks
{
    /// <summary>
    /// Helper class for handling embedded resources.
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// Extracts an embedded resource from the executing assembly.
        /// </summary>
        /// <param name="resourceLocation">The location of the resource in the assembly.</param>
        /// <param name="fileName">The name of the file to create.</param>
        public static void ExtractEmbeddedResource(string resourceLocation, string fileName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation) ?? throw new ArgumentException("No such resource", nameof(resourceLocation));
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                for (int i = 0; i < stream.Length; i++)
                {
                    fileStream.WriteByte((byte)stream.ReadByte());
                }
            }
        }

        /// <summary>
        /// Extracts an embedded resource from the calling assembly.
        /// </summary>
        /// <param name="resourceLocation">The location of the resource in the assembly.</param>
        /// <param name="fileName">The name of the file to create.</param>
        /// <returns>True if the resource was found and extracted, false otherwise.</returns>
        public static bool ExtractEmbeddedResourceFromCallingAssembly(string resourceLocation, string fileName)
        {
            bool resourceFound = true;

            Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceLocation) ?? throw new ArgumentException("No such resource", nameof(resourceLocation));
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                for (int i = 0; i < stream.Length; i++)
                {
                    fileStream.WriteByte((byte)stream.ReadByte());
                }
            }

            return resourceFound;
        }

        /// <summary>
        /// Extracts an embedded resource from a specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to extract the resource from.</param>
        /// <param name="resourceLocation">The location of the resource in the assembly.</param>
        /// <param name="fileName">The name of the file to create.</param>
        public static void ExtractEmbeddedResourceFromAssembly(string assembly, string resourceLocation, string fileName)
        {
            Stream stream = Assembly.LoadFrom(assembly).GetManifestResourceStream(resourceLocation) ?? throw new ArgumentException("No such resource", nameof(resourceLocation));
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                for (int i = 0; i < stream.Length; i++)
                {
                    fileStream.WriteByte((byte)stream.ReadByte());
                }
            }
        }

        /// <summary>
        /// Checks if a resource exists in the executing assembly.
        /// </summary>
        /// <param name="resourceLocation">The location of the resource in the assembly.</param>
        /// <returns>True if the resource exists, false otherwise.</returns>
        public static bool RessourceExistInExecutingAssembly(string resourceLocation)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return Exist(assembly, resourceLocation);
        }

        /// <summary>
        /// Checks if a resource exists in the calling assembly.
        /// </summary>
        /// <param name="resourceLocation">The location of the resource in the assembly.</param>
        /// <returns>True if the resource exists, false otherwise.</returns>
        public static bool RessourceExistInCallingAssembly(string resourceLocation)
        {
            var assembly = Assembly.GetCallingAssembly();
            return Exist(assembly, resourceLocation);
        }

        /// <summary>
        /// Checks if a resource exists in a specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <param name="resourceLocation">The location of the resource in the assembly.</param>
        /// <returns>True if the resource exists, false otherwise.</returns>
        private static bool Exist(Assembly assembly, string resourceLocation)
        {
            string[] resourceNames = assembly.GetManifestResourceNames();
            bool isPathCorrect = false;
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.Equals(resourceLocation, StringComparison.Ordinal))
                {
                    isPathCorrect = true;
                    break;
                }
            }

            return isPathCorrect;
        }

    }
}
