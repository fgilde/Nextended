using System.Management;

namespace Nextended.Core.Helper
{
    public class SystemHelper
    {
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