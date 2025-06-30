using System;
using System.IO;
using System.Reflection;

namespace NbuildTasks.Debug
{
    /// <summary>
    /// Debug utility for examining embedded resources in MSBuild tasks assembly.
    /// This helps troubleshoot resource loading issues in NbuildTasks.
    /// </summary>
    public static class DebugResources
    {
        /// <summary>
        /// Lists all embedded resources in the NbuildTasks assembly.
        /// </summary>
        public static void ListEmbeddedResources()
        {
            var assembly = Assembly.LoadFrom(@"C:\source\ntools\Release\NbuildTasks.dll");
            var resourceNames = assembly.GetManifestResourceNames();

            Console.WriteLine("Embedded Resources in NbuildTasks.dll:");
            Console.WriteLine("=====================================");

            if (resourceNames.Length == 0)
            {
                Console.WriteLine("No embedded resources found.");
                return;
            }

            foreach (var resourceName in resourceNames)
            {
                Console.WriteLine($"  - {resourceName}");

                // Try to get resource info
                try
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            Console.WriteLine($"    Size: {stream.Length} bytes");
                        }
                        else
                        {
                            Console.WriteLine("    Size: Unable to read");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Error reading resource: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Extracts a specific embedded resource to a file for inspection.
        /// </summary>
        /// <param name="resourceName">Name of the embedded resource</param>
        /// <param name="outputPath">Path where to save the extracted resource</param>
        public static void ExtractResource(string resourceName, string outputPath)
        {
            var assembly = Assembly.LoadFrom(@"C:\source\ntools\Release\NbuildTasks.dll");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Console.WriteLine($"Resource '{resourceName}' not found.");
                    return;
                }

                using (var fileStream = File.Create(outputPath))
                {
                    stream.CopyTo(fileStream);
                }

                Console.WriteLine($"Resource '{resourceName}' extracted to '{outputPath}'");
            }
        }

        /// <summary>
        /// Main entry point for debugging resources.
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("NbuildTasks Resource Debugger");
            Console.WriteLine("============================");

            if (args.Length == 0)
            {
                ListEmbeddedResources();
            }
            else if (args.Length == 2 && args[0] == "extract")
            {
                var parts = args[1].Split('|');
                if (parts.Length == 2)
                {
                    ExtractResource(parts[0], parts[1]);
                }
                else
                {
                    Console.WriteLine("Usage for extract: debug-resources extract <resourceName>|<outputPath>");
                }
            }
            else
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  debug-resources                           - List all embedded resources");
                Console.WriteLine("  debug-resources extract <name>|<path>    - Extract resource to file");
                Console.WriteLine("");
                Console.WriteLine("Examples:");
                Console.WriteLine("  debug-resources");
                Console.WriteLine("  debug-resources extract NbuildTasks.Resources.template.txt|template.txt");
            }
        }
    }
}
