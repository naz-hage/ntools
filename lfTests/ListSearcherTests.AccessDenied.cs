using lf;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace lfTests
{
    [TestClass]
    public class ListSearcherAccessDeniedTests
    {
        private string _testRoot = string.Empty;
        private string _protectedDir = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testRoot);
            _protectedDir = Path.Combine(_testRoot, "protected");
            Directory.CreateDirectory(_protectedDir);
            // Remove all access permissions for the current user (Windows only)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var dirInfo = new DirectoryInfo(_protectedDir);
                var security = dirInfo.GetAccessControl();
                security.SetAccessRuleProtection(true, false);
                var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                foreach (FileSystemAccessRule rule in rules)
                {
                    security.RemoveAccessRule(rule);
                }
                dirInfo.SetAccessControl(security);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Try to restore permissions so we can delete the directory
            if (Directory.Exists(_protectedDir))
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var dirInfo = new DirectoryInfo(_protectedDir);
                        var security = dirInfo.GetAccessControl();
                        security.SetAccessRuleProtection(false, false);
                        dirInfo.SetAccessControl(security);
                    }
                }
                catch { /* ignore errors restoring permissions in cleanup */ }
            }
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [TestMethod]
        public void ListFiles_AccessDenied_DoesNotThrowAndPrintsWarning()
        {
            // Arrange
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            ListSearcher.ListFiles(_testRoot, new[] { ".yaml" });

            // Assert
            var output = sw.ToString();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StringAssert.Contains(output, "Access denied to a directory:");
            }
            // Should not throw, and should continue (no files found)
            StringAssert.Contains(output, "No files found with .yaml extension");
        }
    }
}
