using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Nextended.Aspire;

public class DockerHelper
{
    public static bool IsDockerInstalled() => ExecuteCommand("docker --version").Code == 0;

    public static bool IsDockerRunning() => ExecuteCommand("docker info").Code == 0;

    public static void StartDocker()
    {
        Console.WriteLine("Try to start Docker Desktop");

        var res = ExecuteCommand("docker desktop start"); // only works with 4.37 or newer
        bool success = res.Code == 0 && ExecuteCommand("docker desktop").Output != res.Output;
        if (success)
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = ExecuteCommand("com.docker.cli --version");
                if (result.Code == 0)
                {                    
                    ExecuteCommand("com.docker.cli --start");
                }
                else
                {
                    //last fallback find and start executable
                    var dockerPath = FindDockerDesktopWin();
                    if (!string.IsNullOrEmpty(dockerPath) && File.Exists(dockerPath))
                        Process.Start(dockerPath);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ExecuteCommand("sudo systemctl start docker");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ExecuteCommand("open /Applications/Docker.app");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Docker Desktop: {ex.Message}");
        }
    }

    private static string FindDockerDesktopWin()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var dockerPath = Path.Combine(programFiles, "Docker", "Docker", "Docker Desktop.exe");
        if (!File.Exists(dockerPath))
            dockerPath = Path.Combine(programFilesX86, "Docker", "Docker", "Docker Desktop.exe");
        if (!File.Exists(dockerPath))
            dockerPath = Path.Combine(programFiles, "Docker", "Docker Desktop.exe");
        if (!File.Exists(dockerPath))
            dockerPath = Path.Combine(programFilesX86, "Docker", "Docker Desktop.exe");
        if (!File.Exists(dockerPath))
        {
            // find from registry
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var subKey = key.OpenSubKey(subKeyName);                    
                    var displayName = subKey?.GetValue("DisplayName")?.ToString();
                    if (displayName?.Contains("Docker Desktop") == true)
                    {    
                        dockerPath = Path.Combine(subKey?.GetValue("InstallLocation")?.ToString(), "Docker Desktop.exe");
                    }
                }
            }
        }
        return dockerPath;
    }

    public static void EnsureDockerIsRunning()
    {
        if (!IsDockerInstalled() || IsDockerRunning())
            return;

        StartDocker();
    }

    private static (int Code, string Output) ExecuteCommand(string command)
    {
        try
        {
            string output = string.Empty;
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processInfo.FileName = "cmd.exe";
                processInfo.Arguments = $"/c {command}";
            }

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            process?.WaitForExit();
            return (process?.ExitCode ?? -1, output);
        }
        catch
        {
            return (-1, string.Empty);
        }
    }
}
