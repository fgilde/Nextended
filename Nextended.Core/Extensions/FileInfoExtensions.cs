using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Helper;

namespace Nextended.Core.Extensions
{
    public static class FileInfoExtensions
    {
        public static Process Execute(this FileInfo fileInfo, string arguments = "", ScriptExecutionSettings executionSettings = null, CancellationToken cancellationToken = default)
        {
            if(fileInfo.IsExecutable()) 
                return ScriptHelper.ExecuteScript(fileInfo.FullName, arguments, executionSettings ?? ScriptExecutionSettings.DefaultWithCmd, cancellationToken: cancellationToken)?.Process;
            return Process.Start(fileInfo.FullName);
        }

        public static bool IsLockedByProcess(this FileInfo fileInfo)
        {
            return FileHelper.WhoIsLocking(fileInfo.FullName, true).Any();
        }

        public static IList<Process> FindLockingProcesses(this FileInfo fileInfo)
        {
            return FileHelper.WhoIsLocking(fileInfo.FullName, true);
        }
        
        public static string GetShortPath(this FileSystemInfo fileSystemInfo, int length = 30)
        {
            return FileHelper.ToShortPath(fileSystemInfo.FullName, length);
        }

        public static bool IsExecutable(this FileInfo fileInfo)
        {
            return FileHelper.FileIsExecutable(fileInfo.FullName);
        }

        public static void ShowProperties(this FileInfo fileInfo)
        {
            FileHelper.ShowProperties(fileInfo);
        }

        public static string GetRelativePathTo(this FileSystemInfo fileInfo, FileSystemInfo other)
        {
            return FileHelper.GetRelativePath(fileInfo.FullName, fileInfo is DirectoryInfo, other.FullName, other is DirectoryInfo );
        }

        public static string GetRelativePathTo(this FileSystemInfo fileInfo, string referencePath)
        {
            bool isDir = Directory.Exists(referencePath) || !File.Exists(referencePath) && !Path.HasExtension(referencePath);
            return FileHelper.GetRelativePath(fileInfo.FullName, fileInfo is DirectoryInfo, referencePath, isDir);
        }

        public static Task<DirectoryInfo> CopyToAsync(this DirectoryInfo directoryInfo, DirectoryInfo destination,
            bool overwriteExisting = false)
        {
            return Task.Run(() => CopyTo(directoryInfo, destination, overwriteExisting));
        }

        public static DirectoryInfo CopyTo(this DirectoryInfo directoryInfo, DirectoryInfo destinationFolder, bool overwriteExisting)
        {
            return directoryInfo.CopyTo(destinationFolder.FullName, overwriteExisting);
        }

        public static Task<DirectoryInfo> CopyToAsync(this DirectoryInfo directoryInfo, string destinationFolder,
            bool overwriteExisting = false)
        {
            return Task.Run(() => CopyTo(directoryInfo, destinationFolder, overwriteExisting));
        }

        public static DirectoryInfo CopyTo(this DirectoryInfo directoryInfo, string destinationFolder, bool overwriteExisting = false)
        {
            FileHelper.TaskCopyDirectory(directoryInfo.FullName, destinationFolder, overwriteExisting, true, false);
            return new DirectoryInfo(destinationFolder);
        }

        public static string MimeType(this FileInfo fileInfo)
        {
            return FileHelper.GetMimeType(fileInfo.Extension);
        }

        public static string FileTypeDescription(this FileInfo fileInfo)
        {
            return FileHelper.GetFileDescriptionByExtension(fileInfo.Extension, out string _);
        }

        public static string ExtensionFileName(this FileInfo fileInfo)
        {
            FileHelper.GetFileDescriptionByExtension(fileInfo.Extension, out string res);
            return res;
        }

        public static void MoveToRecycleBin(this FileSystemInfo fileInfo)
        {
            FileHelper.MoveToRecycleBin(fileInfo.FullName);
        }

        /// <summary>
        /// Returns a readable filesize string
        /// </summary>
        public static string GetReadableFileSize(this FileInfo fileInfo, bool fullName = false)
        {
            var sizes = new Dictionary<string, string>
            {
                {"B", "Bytes"},
                {"KB", "Kilobytes"},
                {"MB", "Megabytes"},
                {"GB", "Gigabytes"},
                {"TB", "Terabytes"},
                {"PB", "Petabytes"},
                {"EB", "Exabytes"},
                {"ZB", "Zettabytes"},
                {"YB", "Yottabytes"},
                {"BB", "Brontobytes"},
            };

            double len = fileInfo.Length;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Count)
            {
                order++;
                len = len / 1024;
            }

            var size = sizes.ElementAt(order);
            return $"{len:0.##} {(fullName ? size.Value : size.Key)}";
        }
    }
}