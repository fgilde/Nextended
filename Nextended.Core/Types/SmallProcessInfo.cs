using System.Diagnostics;

namespace Nextended.Core.Types
{
    [DebuggerDisplay("{FileName}")]
    public class SmallProcessInfo
    {
        public int Id { get; set; }
        public Process Process { get; set; }
        public string CommandLine { get; set; }
        public string Path { get; set; }
        public string FileName => System.IO.Path.GetFileName(Path);
    }
}