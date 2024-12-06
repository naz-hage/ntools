using System.Runtime.InteropServices;
using System.Security;

namespace GitHubRelease
{
    static public class Credentials
    {
        private static (SecureString owner, SecureString token) GetOwnerAndToken()
        {
            string? owner = Environment.GetEnvironmentVariable("OWNER");
            if (string.IsNullOrEmpty(owner))
            {
                // read owner from file
                using var reader = new StreamReader($"{Environment.GetEnvironmentVariable("USERPROFILE")}\\.owner");
                owner = reader.ReadToEnd();
            }

            string? token = Environment.GetEnvironmentVariable("API_GITHUB_KEY");
            if (string.IsNullOrEmpty(token))
            {
                // read token from file
                using var reader = new StreamReader($"{Environment.GetEnvironmentVariable("USERPROFILE")}\\.git-credentials");
                token = reader.ReadToEnd();
            }

            SecureString secureOwner = new SecureString();
            foreach (char c in owner)
            {
                secureOwner.AppendChar(c);
            }

            SecureString secureToken = new SecureString();
            foreach (char c in token)
            {
                secureToken.AppendChar(c);
            }

            return (secureOwner, secureToken);
        }

        public static string GetOwner()
        {
            return ConvertToUnsecureString(GetOwnerAndToken().owner);
        }

        public static string GetToken()
        {
            return ConvertToUnsecureString(GetOwnerAndToken().token);
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
