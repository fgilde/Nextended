using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Types;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Monitors system processes and raises events when processes are started or stopped.
    /// </summary>
    public class ProcessWatcher : IDisposable
    {
        private CancellationTokenSource tokenSource;
        private IList<SmallProcessInfo> processes;

        /// <summary>
        /// Occurs when new processes are started.
        /// </summary>
        public event EventHandler<List<SmallProcessInfo>> NewProcessesStarted;
        
        /// <summary>
        /// Occurs when processes are stopped.
        /// </summary>
        public event EventHandler<List<SmallProcessInfo>> ProcessesStopped;

        /// <summary>
        /// Initializes a new instance of the ProcessWatcher class.
        /// </summary>
        public ProcessWatcher()
        { }

        /// <summary>
        /// Stops monitoring processes.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
            tokenSource = null;
        }

        public void Start()
        {
            processes = ProcessHelper.GetProcesses();
            tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(WatchProcesses, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void WatchProcesses()
        {
            while (tokenSource != null && !tokenSource.Token.IsCancellationRequested)
            {
                var currentProcesses = ProcessHelper.GetProcesses();
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

        protected virtual void OnNewProcessStarted(List<SmallProcessInfo> e)
        {
            NewProcessesStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessesStopped(List<SmallProcessInfo> e)
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