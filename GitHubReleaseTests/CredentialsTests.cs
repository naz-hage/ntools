using GitHubRelease;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace GitHubReleaseTests
{
    [TestClass]
    public class CredentialsTests
    {
        private const string CredentialName = "API_GITHUB_KEY_TEST";
        private const string TestToken = "test_token";

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public void SaveTokenToCredentialManager_Success()
        {
            // Arrange
            var secureToken = new SecureString();
            foreach (char c in TestToken)
            {
                secureToken.AppendChar(c);
            }

            // Act
            var result = Credentials.SaveTokenToCredentialManager("GitHubRelease", CredentialName, secureToken);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public void GetTokenFromCredentialManager_Success()
        {
            // Arrange
            var secureToken = new SecureString();
            foreach (char c in TestToken)
            {
                secureToken.AppendChar(c);
            }
            Credentials.SaveTokenToCredentialManager("GitHubRelease", CredentialName, secureToken);

            // Act
            var result = Credentials.GetToken("GitHubRelease", CredentialName);

            // Assert
            Assert.AreEqual(TestToken, ConvertToUnsecureString(result));
        }

        private static string ConvertToUnsecureString(SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString) ?? string.Empty;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                secureString.Dispose();
            }
        }
    }
}
