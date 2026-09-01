using System.Runtime.CompilerServices;

// The tests read the scripts and the state text a clone produces. Both are internal on purpose —
// they are how the package works, not what it offers — and both are worth a test.
[assembly: InternalsVisibleTo("Nextended.Aspire.Hosting.DbTools.Tests")]
