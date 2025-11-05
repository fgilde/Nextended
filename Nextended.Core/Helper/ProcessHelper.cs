using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Nextended.Core.Types;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Provides utility methods for working with system processes, including retrieving process information 
    /// such as executable paths and command-line arguments.
    /// </summary>
    public class ProcessHelper
    {
        /// <summary>
        /// Gets a list of all running processes with their executable paths and command-line arguments.
        /// </summary>
        /// <returns>A list of SmallProcessInfo objects containing process details.</returns>
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