using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace Nextended.UI.Helper
{
    ///<summary>
    /// WindowsSecurityHelper
    ///</summary>
    public static class WindowsSecurityHelper
    {
        ///<summary>
        /// SendMessage
        ///</summary>
        [DllImport("user32")]
        public static extern UInt32 SendMessage(IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam);

        internal const int BCM_FIRST = 0x1600;
        internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C);

        private static readonly Version win7Version = new Version(6, 1);

        /// <summary>
        ///  Gibt das Account Image des angemeldeten Benutzers zurück
        /// </summary>
        public static Bitmap GetAccountImage(this WindowsIdentity identity)
        {
            return GetUserAccountImage(identity.Name);
        }

        /// <summary>
        /// Gibt das Account Image des angemeldeten Benutzers zurück
        /// </summary>
        public static Bitmap GetUserAccountImage(string username = "")
        {
            var file = String.IsNullOrEmpty(username) ? GetUserAccountImagePath() : GetUserAccountImagePath(username);
            if(!String.IsNullOrEmpty(file) && File.Exists(file))
            {
                try
                {
                    return Image.FromFile(file) as Bitmap;
                    //return Image.FromFile(file);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetUserDomainName()
        {
            string username = string.Empty;
            if (!String.IsNullOrEmpty(Environment.UserDomainName))
                username += Environment.UserDomainName + @"\";
            username += Environment.UserName;
            return username;
        }

        /// <summary>
        /// Gibt den Pfad des Userimages zurück
        /// </summary>
        public static string GetUserAccountImagePath()
        {
            string tmp = Path.GetTempPath();
            string username = GetUserDomainName().Replace(@"\","+");

            string file = Path.Combine(tmp,username);
            if(File.Exists(file+".bmp"))
                return file + ".bmp";

            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "User Account Pictures");
            if(Directory.Exists(folderPath))
            {
                string defaultImage = Path.Combine(folderPath, "user.bmp");
                if (File.Exists(defaultImage))
                    return defaultImage;
            }

            return string.Empty;
        }

        /// <summary>
        /// Gibt den Pfad des Userimages zurück
        /// </summary>
        public static string GetUserAccountImagePath(string username)
        {
            string tmp = Path.GetTempPath();
            username = username.Replace(@"\","+");
            string file = Path.Combine(tmp, username);
            if (File.Exists(file + ".bmp"))
                return file + ".bmp";

            return string.Empty;
        }

        /// <summary>
        /// Gibt an ob des System Vista oder höher ist
        /// </summary>
        public static bool IsVistaOrHigher
        {
            get
            {
                return Environment.OSVersion.Version.Major >= 6;
            }
        }

        /// <summary>
        /// Gibt an ob des System Windows 7 oder höher ist
        /// </summary>
        public static bool IsWin7OrHigher
        {
            get
            {
                return Environment.OSVersion.Version >= win7Version;
            }
        }

        /// <summary>
        /// Checks if the process is elevated
        /// </summary>
        public static bool IsAdmin
        {
            get
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                if (id != null)
                {
                    var p = new WindowsPrincipal(id);
                    return p.IsInRole(WindowsBuiltInRole.Administrator);
                }
                return true;
            }
        }

        /// <summary>
        /// Add a shield icon to a button
        /// </summary>
        /// <param name="b">The button</param>
        public static void AddShieldToButton(Button b)
        {
            b.FlatStyle = FlatStyle.System;
            SendMessage(b.Handle, BCM_SETSHIELD, 0, 0xFFFFFFFF);
        }

        /// <summary>
        /// Restart the current process with administrator credentials
        /// </summary>
        public static void RestartElevated()
        {
            var startInfo = new ProcessStartInfo
                                {
                                    UseShellExecute = true,
                                    WorkingDirectory = Environment.CurrentDirectory,
                                    FileName = Application.ExecutablePath,
                                    Verb = "runas"
                                };
            try
            {
                Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return; //If cancelled, do nothing
            }

            Application.Exit();
        }


		/// <summary>
		/// CoTaskMemFree
		/// </summary>
		[DllImport("ole32.dll")]
		public static extern void CoTaskMemFree(IntPtr ptr);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct CREDUI_INFO
		{
			public int cbSize;
			public IntPtr hwndParent;
			public string pszMessageText;
			public string pszCaptionText;
			public IntPtr hbmBanner;
		}


		[DllImport("credui.dll", CharSet = CharSet.Auto)]
		private static extern bool CredUnPackAuthenticationBuffer(int dwFlags,
																   IntPtr pAuthBuffer,
																   uint cbAuthBuffer,
																   StringBuilder pszUserName,
																   ref int pcchMaxUserName,
																   StringBuilder pszDomainName,
																   ref int pcchMaxDomainame,
																   StringBuilder pszPassword,
																   ref int pcchMaxPassword);

		[DllImport("credui.dll", CharSet = CharSet.Auto)]
		private static extern int CredUIPromptForWindowsCredentials(ref CREDUI_INFO notUsedHere,
																	 int authError,
																	 ref uint authPackage,
																	 IntPtr InAuthBuffer,
																	 uint InAuthBufferSize,
																	 out IntPtr refOutAuthBuffer,
																	 out uint refOutAuthBufferSize,
																	 ref bool fSave,
																	 int flags);


		/// <summary>
		/// GetCredentialsVistaAndUp
		/// </summary>
		public static NetworkCredential ShowCredentialDialog(string caption, string message)
		{
			CREDUI_INFO credui = new CREDUI_INFO();
			credui.pszCaptionText = caption;
			credui.pszMessageText = message;
			credui.cbSize = Marshal.SizeOf(credui);
			uint authPackage = 0;
			IntPtr outCredBuffer = new IntPtr();
			uint outCredSize;
			bool save = true;
			int result = CredUIPromptForWindowsCredentials(ref credui, 0,ref authPackage,IntPtr.Zero,0,out outCredBuffer,out outCredSize,ref save,1 /* Generic */);

			var usernameBuf = new StringBuilder(100);
			var passwordBuf = new StringBuilder(100);
			var domainBuf = new StringBuilder(100);

			int maxUserName = 100;
			int maxDomain = 100;
			int maxPassword = 100;
			if (result == 0)
			{
				if (CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName,
												   domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
				{
					//TODO: ms documentation says we should call this but i can't get it to work
					//SecureZeroMem(outCredBuffer, outCredSize);

					//clear the memory allocated by CredUIPromptForWindowsCredentials 
					CoTaskMemFree(outCredBuffer);
					var networkCredential = new NetworkCredential()
					{
						UserName = usernameBuf.ToString(),
						Password = passwordBuf.ToString(),
						Domain = domainBuf.ToString()
					};
					return networkCredential;
				}
			}

			return null;
		}


    }  
}
