using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Provides methods for executing scripts and command-line applications with customizable execution settings,
    /// output capture, and error handling.
    /// </summary>
    public class ScriptHelper
    {
        /// <summary>
        /// Asynchronously executes a script or command with the specified arguments and settings.
        /// </summary>
        /// <param name="fileName">The path to the script or executable to run.</param>
        /// <param name="arguments">Command-line arguments to pass to the script.</param>
        /// <param name="settings">Execution settings that control process behavior.</param>
        /// <param name="onDataReceived">Optional callback for standard output data.</param>
        /// <param name="onError">Optional callback for error output data.</param>
        /// <param name="cancellationToken">Cancellation token to stop execution.</param>
        /// <returns>A task that represents the asynchronous operation, containing the execution result.</returns>
        public static Task<ScriptExecutingResult> ExecuteScriptAsync(string fileName, string arguments,
            ScriptExecutionSettings settings,
            Action<string> onDataReceived = null, Action<string> onError = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => ExecuteScript(fileName, arguments, settings, onDataReceived, onError, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Determines whether the specified file is a PowerShell script based on its extension.
        /// </summary>
        /// <param name="filename">The file path to check.</param>
        /// <returns>True if the file is a PowerShell script (.ps1, .psm1, .psd1, .ps1xml); otherwise, false.</returns>
        public static bool IsPowerShell(string filename)
        {
            string[] extensions = { ".ps1", "psm1", "psd1", "ps1xml" };
            var extension = Path.GetExtension(filename);
            return !string.IsNullOrEmpty(extension) && extensions.Contains(extension.ToLower());
        }

        /// <summary>
        /// Synchronously executes a script or command with the specified arguments and settings.
        /// </summary>
        /// <param name="fileName">The path to the script or executable to run.</param>
        /// <param name="arguments">Command-line arguments to pass to the script.</param>
        /// <param name="settings">Execution settings that control process behavior.</param>
        /// <param name="onDataReceived">Optional callback for standard output data.</param>
        /// <param name="onError">Optional callback for error output data.</param>
        /// <param name="cancellationToken">Cancellation token to stop execution.</param>
        /// <returns>The execution result containing the process and success status.</returns>
        public static ScriptExecutingResult ExecuteScript(string fileName, string arguments,
            ScriptExecutionSettings settings,
            Action<string> onDataReceived = null, Action<string> onError = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Process process = null;
            onDataReceived ??= s => Trace.WriteLine(s);
            onError ??= s => Trace.WriteLine(s);

            if (cancellationToken.IsCancellationRequested)
                return ScriptExecutingResult.False();

            if (settings.ExecuteWithCmd)
            {
                arguments = $"/c {fileName} " + arguments;
                fileName = "cmd";
            }

            bool requiresAdminPrivileges = settings.RequiresAdminPrivileges && !SecurityHelper.IsCurrentProcessAdmin();

            bool result = false;
            try
            {
                var workingDirectory = settings.WorkingDirectory ?? Path.GetDirectoryName(fileName);
                //workingDirectory = @"C:\Dev\HEAD\Tests\CP.Air";

                var processInfo = new ProcessStartInfo(fileName) { Verb = settings.RequiresAdminPrivileges ? "runas" : string.Empty };
                if (!string.IsNullOrEmpty(workingDirectory))
                    processInfo.WorkingDirectory = workingDirectory;
                if (settings.IsHidden && !requiresAdminPrivileges)
                {
                    processInfo.UseShellExecute = false;
                    processInfo.RedirectStandardError = true;
                    processInfo.RedirectStandardInput = true;
                    processInfo.RedirectStandardOutput = true;
                    processInfo.CreateNoWindow = true;
                }
                if (!string.IsNullOrEmpty(arguments))
                    processInfo.Arguments = arguments;
                var p = new Process { StartInfo = processInfo };
                process = p;
                cancellationToken.Register(() =>
                {
                    Check.TryCatch<Exception>(() =>
                    {
                        if (!p.HasExited)
                            p.Kill();
                    });
                });
                if (settings.IsHidden)
                {
                    p.OutputDataReceived += (sender, args) =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                            onDataReceived?.Invoke(args.Data);
                    };
                    p.ErrorDataReceived += (sender, args) =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                            onError?.Invoke(args.Data);
                    };
                    //p.EnableRaisingEvents = true;
                    processInfo.StandardOutputEncoding = Encoding.UTF8;
                }

                p.EnableRaisingEvents = true;

                if (cancellationToken.IsCancellationRequested)
                    return ScriptExecutingResult.False(p);

                result = p.Start();
                p.Exited += (sender, args) => result = result && p.ExitCode == 0;
                if (settings.TrackLiveOutput && p.StartInfo.RedirectStandardOutput)
                {
                    p.BeginOutputReadLine();
                    if (settings.WaitForProcessExit)
                        p.WaitForExit();
                }
                else
                {
                    if (settings.IsHidden && p.StartInfo.RedirectStandardOutput)
                    {
                        StreamWriter sw = p.StandardInput;
                        StreamReader sr = p.StandardOutput;
                        StreamReader err = p.StandardError;
                        sw.AutoFlush = true;
                        sw.WriteLine(" ");
                        sw.Close();

                        string output = sr.ReadToEnd();
                        string error = err.ReadToEnd();
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            onDataReceived(output);
                            onError(error);
                        }
                    }
                    if (settings.WaitForProcessExit)
                        p.WaitForExit();
                }

            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
            return ScriptExecutingResult.FromResult(result, process);
        }

      
        public static string PrepareScriptVars(string scriptContent, bool removeDuplicateLines)
        {
            return PrepareBatchScriptVars(scriptContent, removeDuplicateLines);
        }

        private static string PreparePowershellScriptVars(string script, bool removeDuplicateLines)
        {
            var result = script;
            if (removeDuplicateLines)
                return RemoveDuplicateLines(result);
            return result;
        }

        private static string RemoveDuplicateLines(string result)
        {
            var previousLines = new HashSet<string>();
            return new StringBuilder()
                .AppendLines(result.GetLines().Where(line => String.IsNullOrWhiteSpace(line) || Allowed(line) || previousLines.Add(line)))
                .ToString();
        }

        private static bool Allowed(string line)
        {
            return line.Length < 2;
        }

        private static string PrepareBatchScriptVars(string script, bool removeDuplicateLines)
        {
            var pre = new StringBuilder();
            int index = 0;
            var dict = new Dictionary<string, string>();
            var dict2 = new Dictionary<string, string>();
            MatchCollection matchList = Regex.Matches(script, "([\"'])(?:(?=(\\\\?))\\2.)*?\\1");
            var list = matchList.Cast<Match>().Where(match => !string.IsNullOrEmpty(match.Value)).ToList();
            foreach (Match match in list)
            {
                string value = match.Value.Replace("\"", "");
                string key = FindKey(value);
                if (!dict.ContainsValue(value))
                {
                    if (dict.ContainsKey(key))
                        key += "_" + (++index);
                    dict.Add(key, value);
                }
                else
                {
                    if (!dict2.ContainsKey(key))
                        dict2.Add(key, value);
                    if (!dict.ContainsKey(key))
                        dict.Remove(key);
                }
            }
            foreach (var pair in dict2)
            {
                pre.AppendLine($"SET {pair.Key}={pair.Value}");
                script = script.Replace(pair.Value, $"%{pair.Key}%");
            }
            var result = pre.AppendLine() + script;
            return removeDuplicateLines ? RemoveDuplicateLines(result) : result;
        }

        private static string FindKey(string value)
        {
            string res = value.Replace("\"", "");
            if (File.Exists(res))
                res = $"{Path.GetFileName(res)}";
            else if (res.Contains(" "))
            {
                var indexOf = res.IndexOf(" ");
                res = res.Substring(0, indexOf);
            }
            return res.Replace(".", "_").Replace(" ", "").Replace(":", "_").Replace("\\", "").ToUpper();
        }

    }

    /// <summary>
    /// Represents the result of a script execution, containing the process and execution status.
    /// </summary>
    public class ScriptExecutingResult
    {
        private ScriptExecutingResult()
        { }

        /// <summary>
        /// Gets a value indicating whether the process execution was successful.
        /// </summary>
        public bool ProcessResult { get; private set; }
        
        /// <summary>
        /// Gets the Process object associated with the script execution.
        /// </summary>
        public Process Process { get; private set; }

        /// <summary>
        /// Creates a ScriptExecutingResult from the specified result and process.
        /// </summary>
        /// <param name="result">True if the execution was successful; otherwise, false.</param>
        /// <param name="process">The process that was executed.</param>
        /// <returns>A new ScriptExecutingResult instance.</returns>
        public static ScriptExecutingResult FromResult(bool result, Process process = null)
        {
            return new ScriptExecutingResult { Process = process, ProcessResult = result };
        }

        public static ScriptExecutingResult False(Process process = null)
        {
            return FromResult(false, process);
        }

        public static ScriptExecutingResult True(Process process = null)
        {
            return FromResult(true, process);
        }

        public override string ToString()
        {
            return ProcessResult.ToString();
        }
    }

}