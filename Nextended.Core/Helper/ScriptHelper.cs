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
    public class ScriptHelper
    {
        public static Task<ScriptExecutingResult> ExecuteScriptAsync(string fileName, string arguments,
            ScriptExecutionSettings settings,
            Action<string> onDataReceived = null, Action<string> onError = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => ExecuteScript(fileName, arguments, settings, onDataReceived, onError, cancellationToken), cancellationToken);
        }

        public static bool IsPowerShell(string filename)
        {
            string[] extensions = { ".ps1", "psm1", "psd1", "ps1xml" };
            var extension = Path.GetExtension(filename);
            return !string.IsNullOrEmpty(extension) && extensions.Contains(extension.ToLower());
        }

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

    public class ScriptExecutingResult
    {
        private ScriptExecutingResult()
        { }

        public bool ProcessResult { get; private set; }
        public Process Process { get; private set; }

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