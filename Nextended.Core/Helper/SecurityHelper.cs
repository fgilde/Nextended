using System.Runtime.InteropServices;

namespace Nextended.Core.Helper
{
    public class SecurityHelper
    {
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        public static bool IsCurrentProcessAdmin()
        {
            return IsUserAnAdmin();
        }
    }
}