using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Helper
{

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string CommandLine { get; set; }
        public Process Process { get; set; }
    }

    public class ProcessWatcher : IDisposable
    {
        private CancellationTokenSource tokenSource;
        private List<ProcessInfo> processes;

        public event EventHandler<List<ProcessInfo>> NewProcessesStarted;
        public event EventHandler<List<ProcessInfo>> ProcessesStopped;

        public ProcessWatcher()
        { }

        public void Stop()
        {
            tokenSource.Cancel();
            tokenSource = null;
        }

        public void Start()
        {
            processes = GetProcesses();
            tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(WatchProcesses, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static List<ProcessInfo> GetProcesses()
        {
            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var results = searcher.Get())
            {
                var query = from p in Process.GetProcesses()
                            join mo in results.Cast<ManagementObject>()
                                on p.Id equals (int)(uint)mo["ProcessId"]
                            select new ProcessInfo
                            {
                                Id = p.Id,
                                Process = p,
                                Path = (string)mo["ExecutablePath"],
                                CommandLine = (string)mo["CommandLine"],
                            };
                return query.ToList();
            }
        }

        private void WatchProcesses()
        {
            while (tokenSource != null && !tokenSource.Token.IsCancellationRequested)
            {
                var currentProcesses = GetProcesses();
                if (currentProcesses.Count != processes.Count)
                {
                    var newProcesses = currentProcesses.Select(p => p.Id).Except(processes.Select(p => p.Id)).ToList();
                    if (newProcesses.Any())
                        OnNewProcessStarted(currentProcesses.Where(p => newProcesses.Contains(p.Id)).ToList());
                    var goneProcesses = processes.Select(p => p.Id).Except(currentProcesses.Select(p => p.Id)).ToList();
                    if (goneProcesses.Any())
                        OnProcessesStopped(processes.Where(p => goneProcesses.Contains(p.Id)).ToList());

                }
                processes = currentProcesses;
            }
        }

        protected virtual void OnNewProcessStarted(List<ProcessInfo> e)
        {
            NewProcessesStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessesStopped(List<ProcessInfo> e)
        {
            ProcessesStopped?.Invoke(this, e);
        }

        public void Dispose()
        {
            Stop();
            tokenSource?.Dispose();
        }
    }
}