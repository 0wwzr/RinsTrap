using System;
using System.Runtime.InteropServices;
using System.Security;

namespace RinsTrap.Integrations
{
    public static class RobloxCookieManager
    {
        private const string RobloxTarget = "Roblox";

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, int type, int reserved, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, int type, int reserved);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredFree(IntPtr credentialPtr);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        private const int CRED_TYPE_GENERIC = 1;
        private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

        public static bool SaveCookie(string cookie, string userName = "Roblox")
        {
            try
            {
                var credential = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = RobloxTarget,
                    UserName = userName,
                    Comment = "Roblox .ROBLOSECURITY cookie for auto-login",
                    Persist = CRED_PERSIST_LOCAL_MACHINE,
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero
                };

                byte[] cookieBytes = System.Text.Encoding.UTF8.GetBytes(cookie);
                credential.CredentialBlobSize = (uint)cookieBytes.Length;
                credential.CredentialBlob = Marshal.StringToHGlobalUni(cookie);

                bool result = CredWrite(ref credential, 0);
                Marshal.FreeHGlobal(credential.CredentialBlob);

                if (result)
                {
                    App.Logger.WriteLine("RobloxCookieManager", "Cookie saved to Windows Credential Manager");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    App.Logger.WriteLine("RobloxCookieManager", $"Failed to save cookie. Error: {error}");
                }

                return result;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RobloxCookieManager", $"Exception saving cookie: {ex.Message}");
                return false;
            }
        }

        public static string? ReadCookie()
        {
            try
            {
                IntPtr credentialPtr;
                bool result = CredRead(RobloxTarget, CRED_TYPE_GENERIC, 0, out credentialPtr);

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 1168) // ERROR_NOT_FOUND
                    {
                        return null;
                    }
                    App.Logger.WriteLine("RobloxCookieManager", $"Failed to read cookie. Error: {error}");
                    return null;
                }

                CREDENTIAL credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                string cookie = Marshal.PtrToStringUni(credential.CredentialBlob, (int)(credential.CredentialBlobSize / 2));
                CredFree(credentialPtr);

                return cookie;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RobloxCookieManager", $"Exception reading cookie: {ex.Message}");
                return null;
            }
        }

        public static bool DeleteCookie()
        {
            try
            {
                bool result = CredDelete(RobloxTarget, CRED_TYPE_GENERIC, 0);
                if (result)
                {
                    App.Logger.WriteLine("RobloxCookieManager", "Cookie deleted from Windows Credential Manager");
                }
                return result;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RobloxCookieManager", $"Exception deleting cookie: {ex.Message}");
                return false;
            }
        }

        public static bool HasCookie()
        {
            return ReadCookie() != null;
        }

        // Also set the cookie in the Windows Internet settings (WinInet) for browser-based auth
        public static void SetWinInetCookie(string cookie)
        {
            try
            {
                InternetSetCookie("https://www.roblox.com", ".ROBLOSECURITY", cookie);
                InternetSetCookie("https://roblox.com", ".ROBLOSECURITY", cookie);
                InternetSetCookie("https://users.roblox.com", ".ROBLOSECURITY", cookie);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RobloxCookieManager", $"Exception setting WinInet cookie: {ex.Message}");
            }
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetSetCookie(string lpszUrl, string lpszCookieName, string lpszCookieData);
    }
}