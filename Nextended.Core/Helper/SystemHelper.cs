using System.Management;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Provides system-level utility methods for querying hardware and environment information.
    /// </summary>
    public class SystemHelper
    {
        /// <summary>
        /// Determines whether the current system is running in a virtual machine.
        /// </summary>
        /// <returns>True if running in a virtual machine (VMware or Microsoft VM); otherwise, false.</returns>
		public static bool IsVirtualMachine()
        {
            const string microsoftcorporation = "microsoft corporation";
            const string vmware = "vmware";

            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                string manufacturer = item["Manufacturer"].ToString().ToLower();
                // Check the Manufacturer (eg: vmware, inc)
                if (manufacturer.Contains(microsoftcorporation) || manufacturer.Contains(vmware))
                {
                    return true;
                }

                // Also, check the model (eg: VMware Virtual Platform)
                if (item["Model"] != null)
                {
                    string model = item["Model"].ToString().ToLower();
                    if (model.Contains(microsoftcorporation) || model.Contains(vmware))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
	}
}