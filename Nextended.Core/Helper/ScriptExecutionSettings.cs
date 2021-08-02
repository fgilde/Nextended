namespace Nextended.Core.Helper
{
    public class ScriptExecutionSettings
    {
        //bool hidden = true, bool waitForProcessExit = true, 

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public ScriptExecutionSettings() : this(true, true, true)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public ScriptExecutionSettings(bool isHidden, bool trackLiveOutput, bool waitForProcessExit)
        {
            IsHidden = isHidden;
            TrackLiveOutput = trackLiveOutput;
            WaitForProcessExit = waitForProcessExit;
            RequiresAdminPrivileges = false;
        }

        public bool RequiresAdminPrivileges { get; set; }
        public bool IsHidden { get; set; }
        public bool TrackLiveOutput { get; set; }
        public bool WaitForProcessExit { get; set; }
        public string WorkingDirectory { get; set; }
        public bool IgnoreExceptions { get; set; }
        public bool ExecuteWithCmd { get; set; }

        public static ScriptExecutionSettings Default => new ScriptExecutionSettings(true, true, true);

        public static ScriptExecutionSettings DefaultWithCmd => new ScriptExecutionSettings(true, true, true) { ExecuteWithCmd = true };

        public static ScriptExecutionSettings NormalProcess => new ScriptExecutionSettings(false, false, false);
        
        public static ScriptExecutionSettings OneOutputStream => new ScriptExecutionSettings(true, false, true);
        
        public static ScriptExecutionSettings OneSafeOutputStream => new ScriptExecutionSettings(true, false, true) { IgnoreExceptions = true };
    }
}