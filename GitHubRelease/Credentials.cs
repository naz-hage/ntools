using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace GitHubRelease
{
    /// <summary>
    /// Provides methods for securely retrieving and storing credentials, such as the GitHub repository owner and API token.
    /// </summary>
    static public class Credentials
    {
        /// <summary>
        /// Retrieves the GitHub repository owner and API token.
        /// </summary>
        /// <returns>A tuple containing the owner and token as SecureString objects.</returns>
        /// <remarks>
        /// The owner is retrieved from the OWNER environment variable.
        /// The token is retrieved from the Windows Credential Manager if running on Windows, otherwise from the API_GITHUB_KEY environment variable.
        /// </remarks>
        private static (SecureString owner, SecureString token) GetOwnerAndToken()
        {
            string? owner = Environment.GetEnvironmentVariable("OWNER");
            if (owner == null)
            {
                throw new ArgumentNullException("OWNER", "Environment variable 'OWNER' is required");
            }

            SecureString secureOwner = CreateSecureString(owner);
            if (secureOwner == null || secureOwner.Length == 0)
            {
                throw new ArgumentException("The OWNER environment variable cannot be empty");
            }

            SecureString secureToken;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                secureToken = GetToken("GitHubRelease", "API_GITHUB_KEY");

                // If the token is not found in the Credential Manager, try to get it from the environment variable
                if (secureToken == null || secureToken.Length == 0)
                {
                    secureToken = GetTokenFromEnvironmentVariable();
                }
            }
            else
            {
                // Used in GitHub Actions Runner
                secureToken = GetTokenFromEnvironmentVariable();
            }

            return (secureOwner, secureToken);
        }

        
        /// <summary>
        /// Saves a token to the Windows Credential Manager.
        /// </summary>
        /// <param name="target">The target application name.</param>
        /// <param name="credentialName">The name of the credential to save.</param>
        /// <param name="token">A SecureString containing the token to save.</param>
        /// <returns>True if the token was saved successfully, false otherwise.</returns>
        /// <remarks>
        /// This method is only supported on Windows platforms. It uses the CredWrite function from Advapi32.dll to write the credential.
        /// </remarks>
        [SupportedOSPlatform("windows")]
        public static bool SaveTokenToCredentialManager(string target, string credentialName, SecureString token)
        {
            byte[] tokenBytes = Encoding.UTF8.GetBytes(ConvertToUnsecureString(token));

            IntPtr buffer = Marshal.AllocHGlobal(tokenBytes.Length);
            Marshal.Copy(tokenBytes, 0, buffer, tokenBytes.Length);

            NativeMethods.CREDENTIAL cred = new NativeMethods.CREDENTIAL
            {
                Flags = 0,
                Type = NativeMethods.CRED_TYPE_GENERIC,
                TargetName = Marshal.StringToHGlobalUni(credentialName),
                Comment = IntPtr.Zero,
                LastWritten = new System.Runtime.InteropServices.ComTypes.FILETIME(),
                CredentialBlobSize = tokenBytes.Length,
                CredentialBlob = buffer,
                Persist = NativeMethods.CRED_PERSIST_LOCAL_MACHINE, // Or CRED_PERSIST_USER
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = IntPtr.Zero,
                UserName = IntPtr.Zero
            };

            bool success = NativeMethods.CredWrite(ref cred, 0);

            Marshal.FreeHGlobal(cred.TargetName);
            Marshal.FreeHGlobal(buffer);

            if (!success)
            {
                Console.WriteLine($"Failed to write credential '{credentialName}' to Credential Manager. Error code: {Marshal.GetLastWin32Error()}");
            }

            return success;
        }

        /// <summary>
        /// Retrieves a token from the API_GITHUB_KEY environment variable.
        /// </summary>
        /// <returns>A SecureString containing the token.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the API_GITHUB_KEY environment variable is not set.</exception>
        /// <exception cref="ArgumentException">Thrown if the API_GITHUB_KEY environment variable is empty.</exception>
        private static SecureString GetTokenFromEnvironmentVariable()
        {
            string? token = Environment.GetEnvironmentVariable("API_GITHUB_KEY");
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("Environment variable 'API_GITHUB_KEY' is required");
            }

            SecureString secureToken = CreateSecureString(token);
            if (secureToken == null || secureToken.Length == 0)
            {
                throw new ArgumentException("The API_GITHUB_KEY environment variable cannot be empty");
            }

            return secureToken;
        }

        /// <summary>
        /// Gets the GitHub repository owner.
        /// </summary>
        /// <returns>The GitHub repository owner.</returns>
        public static string GetOwner()
        {
            return ConvertToUnsecureString(GetOwnerAndToken().owner);
        }

        /// <summary>
        /// Gets the GitHub API token.
        /// </summary>
        /// <returns>The GitHub API token.</returns>
        public static string GetToken()
        {
            return ConvertToUnsecureString(GetOwnerAndToken().token);
        }

/// <summary>
        /// Retrieves a token from the Windows Credential Manager.
        /// </summary>
        /// <param name="target">The target application name.</param>
        /// <param name="credentialName">The name of the credential to retrieve.</param>
        /// <returns>A SecureString containing the token.</returns>
        /// <remarks>
        /// This method is only supported on Windows platforms. It uses the CredRead function from Advapi32.dll to read the credential.
        /// </remarks>
        [SupportedOSPlatform("windows")]
        public static SecureString GetToken(string target, string credentialName)
        {
            SecureString secureToken = new SecureString();
            bool success = NativeMethods.CredRead(credentialName, NativeMethods.CRED_TYPE_GENERIC, 0, out var credPtr);

            if (success)
            {
                try
                {
                    var cred = Marshal.PtrToStructure<NativeMethods.CREDENTIAL>(credPtr);
                    if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
                    {
                        byte[] credentialBytes = new byte[cred.CredentialBlobSize];
                        Marshal.Copy(cred.CredentialBlob, credentialBytes, 0, cred.CredentialBlobSize);
                        string token = Encoding.UTF8.GetString(credentialBytes);

                        secureToken = CreateSecureString(token);
                    }
                }
                finally
                {
                    NativeMethods.CredFree(credPtr);
                }
            }
            else
            {
                Console.WriteLine($"Failed to read credential '{credentialName}' from Credential Manager. Error code: {Marshal.GetLastWin32Error()}");
                // Optionally, throw an exception or return an empty SecureString
            }

            return secureToken;
        }

        /// <summary>
        /// Converts a SecureString to an unsecure string.
        /// </summary>
        /// <param name="secureString">The SecureString to convert.</param>
        /// <returns>The unsecure string.</returns>
        /// <remarks>
        /// This method should be used with caution, as it exposes the contents of the SecureString in memory.
        /// </remarks>
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

        /// <summary>
        /// Creates a SecureString from a string.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The SecureString.</returns>
        private static SecureString CreateSecureString(string input)
        {
            SecureString secureString = new SecureString();
            foreach (char c in input)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }

        /// <summary>
        /// Provides access to native Windows API methods.
        /// </summary>
        private static class NativeMethods
        {
            public const int CRED_TYPE_GENERIC = 1;
            public const int CRED_PERSIST_LOCAL_MACHINE = 2;
            // public const int CRED_PERSIST_USER = 1;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct CREDENTIAL
            {
                public int Flags;
                public int Type;
                public IntPtr TargetName;
                public IntPtr Comment;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
                public int CredentialBlobSize;
                public IntPtr CredentialBlob;
                public int Persist;
                public int AttributeCount;
                public IntPtr Attributes;
                public IntPtr TargetAlias;
                public IntPtr UserName;
            }

            [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CredRead(string targetName, int type, int flags, out IntPtr credentialPtr);

            [DllImport("Advapi32.dll", SetLastError = true)]
            public static extern bool CredFree(IntPtr credentialPtr);

            [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CredWrite(ref CREDENTIAL credential, int flags);
        }
    }
}