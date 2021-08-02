using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Nextended.Core.Types;

namespace Nextended.Core.Helper
{
    public class ProcessHelper
    {
        public static IList<SmallProcessInfo> GetProcesses()
        {
            const string wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var results = searcher.Get())
            {
                return (from p in Process.GetProcesses()
                    join mo in results.Cast<ManagementObject>()
                        on p.Id equals (int)(uint)mo["ProcessId"]
                    select new SmallProcessInfo
                    {
                        Id = p.Id,
                        Process = p,
                        Path = (string)mo["ExecutablePath"],
                        CommandLine = (string)mo["CommandLine"],
                    }).ToList();
            }
        }
    }
}