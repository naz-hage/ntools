using System.IO;
using System.Reflection;

namespace Nbuild
{
    public class ResourceHelper
    {
        public static void ExtractEmbeddedResource(string outputDir, string resourceLocation, string fileName)
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation) ?? throw new ArgumentException("No such resource", nameof(resourceLocation));
            using var fileStream = new FileStream(Path.Combine(outputDir, fileName), FileMode.Create);
            for (int i = 0; i < stream.Length; i++)
            {
                fileStream.WriteByte((byte)stream.ReadByte());
            }
            fileStream.Close();
        }

        public static void ExtractEmbeddedResourceFromAssembly(string assembly, string resourceLocation, string fileName)
        {
            using Stream stream = Assembly.LoadFile(assembly).GetManifestResourceStream(resourceLocation) ?? throw new ArgumentException("No such resource", nameof(resourceLocation));
            using var fileStream = new FileStream(fileName, FileMode.Create);
            for (int i = 0; i < stream.Length; i++)
            {
                fileStream.WriteByte((byte)stream.ReadByte());
            }
            fileStream.Close();
        }
    }
}
