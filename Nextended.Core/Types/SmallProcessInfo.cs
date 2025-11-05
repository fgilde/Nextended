using System.Diagnostics;

namespace Nextended.Core.Types
{
    /// <summary>
    /// Represents basic information about a running process, including its ID, executable path, and command-line arguments.
    /// </summary>
    [DebuggerDisplay("{FileName}")]
    public class SmallProcessInfo
    {
        /// <summary>
        /// Gets or sets the process ID.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the Process object.
        /// </summary>
        public Process Process { get; set; }
        
        /// <summary>
        /// Gets or sets the command-line arguments used to start the process.
        /// </summary>
        public string CommandLine { get; set; }
        
        /// <summary>
        /// Gets or sets the full path to the process executable.
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Gets the file name (without path) of the process executable.
        /// </summary>
        public string FileName => System.IO.Path.GetFileName(Path);
    }
}