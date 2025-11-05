using System.Runtime.InteropServices;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Provides security-related utility methods for checking user privileges and permissions.
    /// </summary>
    public class SecurityHelper
    {
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        /// <summary>
        /// Determines whether the current process is running with administrator privileges.
        /// </summary>
        /// <returns>True if the current process has administrator privileges; otherwise, false.</returns>
        public static bool IsCurrentProcessAdmin()
        {
            return IsUserAnAdmin();
        }
    }
}