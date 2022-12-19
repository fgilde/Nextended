using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace Nextended.Core.Helper
{

    public class FileHelper
    {

        #region Structs

        [DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(
            string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        private enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }


        /// <summary>
        /// Struktur zur Übergabe an SHFileOperation
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct Shfileopstruct
        {
            public IntPtr hwnd;
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        /// <summary>
        /// Structure that encapsulates basic information of icon embedded in a file.
        /// </summary>
        public struct EmbeddedIconInfo
        {
            public string FileName;
            public int IconIndex;
        }

        [Serializable]
        public struct ShellExecuteInfo
        {
            public int Size;
            public uint Mask;
            public IntPtr Hwnd;
            public string Verb;
            public string File;
            public string Parameters;
            public string Directory;
            public uint Show;
            public IntPtr InstApp;
            public IntPtr IDList;
            public string Class;
            public IntPtr HkeyClass;
            public uint HotKey;
            public IntPtr Icon;
            public IntPtr Monitor;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ShfileInfo
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        #endregion

        #region Konstanten
        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        private const uint swNormal = 1;
        private const int swShow = 5;
        private const uint seeMaskInvokeidlist = 12;
        /* Konstanten für SHFileOperation (aus ShellAPI.h) */
        private const int foCopy = 0x0002;            // Datei oder Ordner kopieren
        private const int fofAllowundo = 0x0040;      // Rückgängigmachen erlauben
        private const int fofNoconfirmation = 0x0010; // Keine Nachfrage beim Anwender
        private const int foDelete = 0x0003;          // Datei/Ordner löschen 
        private const int foMove = 0x0001;            // Verschieben

        #endregion

        #region PInvoke APIs

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
                                              UInt32 nFiles,
                                              string[] rgsFilenames,
                                              UInt32 nApplications,
                                              [In] RM_UNIQUE_PROCESS[] rgApplications,
                                              UInt32 nServices,
                                              string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle,
                                    out uint pnProcInfoNeeded,
                                    ref uint pnProcInfo,
                                    [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
                                    ref uint lpdwRebootReasons);

        /// <summary>
        /// AOI-Funktion für verschiedene Dateioperationen
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHFileOperation(ref Shfileopstruct fileOp);

        // Code For OpenWithDialog Box

        [DllImport("shell32.dll", SetLastError = true)]
        extern public static bool
               ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref ShfileInfo psfi, uint cbSizeFileInfo, uint uFlags);

        /// <summary>
        /// Deklaration der API-Funktion PathRelativePathTo
        /// </summary>
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathRelativePathTo(StringBuilder pszPath,
                                              string pszFrom, uint dwAttrFrom, string pszTo, uint dwAttrTo);



        [DllImport("shell32.dll", EntryPoint = "ExtractIconA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr ExtractIcon(int hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        public static extern int DestroyIcon(IntPtr hIcon);

        #endregion

        public static string NextAvailableFilename(string path, string numberPattern = " ({0})")
        {
            // Short-cut if already available
            if (!File.Exists(path))
                return path;

            return Path.HasExtension(path) ? GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path)), numberPattern)) : GetNextFilename(path + numberPattern);

        }

        private static string GetNextFilename(string pattern)
        {
            string tmp = string.Format(pattern, 1);

            if (!File.Exists(tmp))
                return tmp; // short-circuit if no matches

            int min = 1, max = 2; // min is inclusive, max is exclusive/untested

            while (File.Exists(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (File.Exists(string.Format(pattern, pivot)))
                    min = pivot;
                else
                    max = pivot;
            }

            return string.Format(pattern, max);
        }

        public static void CreateSymbolicLink(SymbolLinkInfo info)
        {
            CreateSymbolicLink(info.LinkName, info.Target);
        }

        public static void CreateSymbolicLink(string linkName, string target)
        {
            SymbolicLink linkType = string.IsNullOrEmpty(Path.GetExtension(linkName)) ? SymbolicLink.Directory : SymbolicLink.File;
            if (linkType == SymbolicLink.Directory)
            {
                var info = new DirectoryInfo(linkName);
                if (info.Parent != null && !Directory.Exists(info.Parent.FullName))
                    Directory.CreateDirectory(info.Parent.FullName);
            }
            else if (linkType == SymbolicLink.File)
            {
                var info = new FileInfo(linkName);
                if (info.Directory != null && !info.Directory.Exists)
                    Directory.CreateDirectory(info.Directory.FullName);
            }
            try
            {
                CreateSymbolicLink(linkName, target, linkType);
            }
            catch { }
        }

        public static void RemoveSymbolicLink(string linkName)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(linkName)) && File.Exists(linkName))
            {
                File.Delete(linkName);
            }
            else if (Directory.Exists(linkName) && !Directory.GetFiles(linkName).Any())
            {
                try
                {
                    Directory.Delete(linkName);
                }
                catch (Exception)
                { }
            }
        }
        
        public static bool IsSubPathOf(string path, string baseDirPath)
        {
            if (path == baseDirPath)
                return false;
            string normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
                .EnsureEndsWith("\\"));

            string normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
                .EnsureEndsWith("\\"));

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
        }
        
        public static string EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static void SetReadOnlyAttribute(string fullName, bool readOnly)
        {
            FileInfo filePath = new FileInfo(fullName);
            FileAttributes attribute;
            if (readOnly)
                attribute = filePath.Attributes | FileAttributes.ReadOnly;
            else
                attribute = (FileAttributes)(filePath.Attributes - FileAttributes.ReadOnly);

            File.SetAttributes(filePath.FullName, attribute);
        }
        
        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>		
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)		
        /// </remarks>
        public static List<Process> WhoIsLocking(string path, bool ignoreExceptions)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            List<Process> processes = new List<Process>();

            int res = RmStartSession(out handle, 0, key);
            if (res != 0 && !ignoreExceptions)
                throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0,
                    pnProcInfo = 0,
                    lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException)
                            {
                            }
                        }
                    }
                    else if (!ignoreExceptions) throw new Exception("Could not list processes locking resource.");
                }
                else if (res != 0 && !ignoreExceptions)
                    throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            catch (Exception)
            {
                if (!ignoreExceptions)
                    throw;
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }

        public static IEnumerable<FileInfo> GetAllFiles(string path, params string[] filters)
        {
            return GetAllFiles(path, null, null, filters);
        }

        /// <summary>
        /// Gets all files.
        /// </summary>
        public static IEnumerable<FileInfo> GetAllFiles(string path, Func<DirectoryInfo, bool> directoryConditionFn,
            Func<FileInfo, bool> fileConditionFn, params string[] filters)
        {
            var root = new DirectoryInfo(path);
            var nonHiddenDirectories = root.EnumerateDirectories("*", SearchOption.AllDirectories)
                .Where(d => (d.Attributes & FileAttributes.Hidden) == 0 && (directoryConditionFn == null || directoryConditionFn(d)));
            return nonHiddenDirectories.SelectMany(d => filters.SelectMany(d.EnumerateFiles))
                .Where(f => (f.Attributes & FileAttributes.Hidden) == 0 && (fileConditionFn == null || fileConditionFn(f)));
        }

        /// <summary>
        /// Prüft, ob eine Datei ausfürbar ist (.exe, .bat, etc.)
        /// </summary>
        public static bool FileIsExecutable(string path)
        {
            var fi = new ShfileInfo();
            const int shgfiExetype = 0x000002000;

            IntPtr res = SHGetFileInfo(
                path,
                0,
                ref fi,
                (uint)Marshal.SizeOf(fi),
                shgfiExetype);

            return (res != IntPtr.Zero);
        }

        public static void ShowProperties(string path)
        {
            var fi = new FileInfo(path);
            ShowProperties(fi);
        }

        public static void ShowProperties(FileInfo fi)
        {
            var info = new ShellExecuteInfo();
            info.Size = Marshal.SizeOf(info);
            info.Verb = "properties";
            info.File = fi.Name;
            info.Directory = fi.DirectoryName;
            info.Show = swShow;
            info.Mask = seeMaskInvokeidlist;
            ShellExecuteEx(ref info);
        }


        /// <summary>
        /// Datei öffnen als
        /// </summary>
        public static void OpenAs(string file)
        {
            var sei = new ShellExecuteInfo();
            sei.Size = Marshal.SizeOf(sei);
            sei.Verb = "openas";
            sei.File = file;
            sei.Show = swNormal;
            if (!ShellExecuteEx(ref sei))
                throw new System.ComponentModel.Win32Exception();
        }

        /// <summary>
        /// Diese Funktion kürzt einen Pfad ab so das aus
        /// "C:\Windows\System32\Test\Test.dll" dann "C:\Windows\...\Test.dll" wird.
        /// </summary>
        /// <param name="path">Der Pfad, der gekürzt zurückgegeben werden soll.</param>
        /// <param name="length">Die gewünschte Länge, die nicht überschritten werden darf.</param>
        public static string ToShortPath(string path, int length)
        {
            string[] pathParts = path.Split('\\');
            var pathBuild = new StringBuilder(path.Length);
            string lastPart = pathParts[pathParts.Length - 1];
            string prevPath = String.Empty;

            //Erst prüfen ob der komplette String evtl. bereits kürzer als die Maximallänge ist
            if (path.Length < length)
                return path;

            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                pathBuild.Append(pathParts[i] + @"\");
                if ((pathBuild + @"...\" + lastPart).Length >= length)
                    return prevPath;
                prevPath = pathBuild + @"...\" + lastPart;
            }
            return prevPath;
        }

        /// <summary>
        /// Ermittelt den relativen Pfad eines absoluten Pfades
        /// </summary>
        /// <param name="absolutePath">Der absolute Pfad</param>
        /// <param name="absolutePathIsDirectory">Gibt an, ob es sich bei dem absoluten Pfad um ein Verzeichnis handelt (anderenfalls handelt es sich um eine Datei)</param>
        /// <param name="referencePath">Der Pfad, auf den sich der relative Pfad bezieht</param>
        /// <param name="referencePathIsDirectory">Gibt an, ob es sich bei dem Bezugs-Pfad um ein Verzeichnis handelt (anderenfalls handelt es sich um eine Datei)</param>
        /// <returns>Gibt den relativen Pfad zurück</returns>
        /// <exception cref="IOException">Wird geworfen falls der absolute und der Referenz-Pfad keine gemeinsame Basis besitzen</exception>
        public static string GetRelativePath(string absolutePath,
                                             bool absolutePathIsDirectory, string referencePath,
                                             bool referencePathIsDirectory)
        {
            // Die Pfadangaben normalisieren für den Fall, dass
            // ..- und .-Angaben enthalten sind
            absolutePath = Path.GetFullPath(absolutePath);
            referencePath = Path.GetFullPath(referencePath);

            const uint fileAttributeDirectory = 0x10;
            const int maxPath = 260;

            // PathRelativePathTo aufrufen
            var relativePath = new StringBuilder(maxPath);
            if (PathRelativePathTo(relativePath, referencePath,
                                   (referencePathIsDirectory ? fileAttributeDirectory : 0),
                                   absolutePath,
                                   (absolutePathIsDirectory ? fileAttributeDirectory : 0)))
            {
                return relativePath.ToString();
            }
            return absolutePath;
        }

        /// <summary>
        /// Ermittelt für einen gegebenen relativen einen absoluten Pfad
        /// </summary>
        /// <param name="relativePath">Der relative Pfad</param>
        /// <param name="referencePath">Der Pfad, auf den sich der relative Pfad bezieht</param>
        /// <returns>Gibt den ermittelten absoluten Pfad zurück</returns>
        public static string GetAbsolutePath(string relativePath,
                                             string referencePath)
        {
            if (referencePath.EndsWith("\\") == false)
            {
                referencePath += "\\";
            }
            return Path.GetFullPath(referencePath + relativePath);
        }

        /// <summary>
        /// Gibt an ob ein Verzeichnis existiert, und kann dabei einen relativen bzug berücksichtigen
        /// </summary>
        public static bool DirectoryExists(string path, string relativeTo = null)
        {
            if (!String.IsNullOrEmpty(relativeTo) && (Directory.Exists(relativeTo) || File.Exists(relativeTo)))
            {
                var p = path;
                try
                {
                    path = GetAbsolutePath(path, relativeTo);
                }
                catch
                {
                    path = p;
                }
            }
            return Directory.Exists(path);
        }

        /// <summary>
        /// Gibt an ob eine Datei existiert, und kann dabei einen relativen bzug berücksichtigen
        /// </summary>
        public static bool FileExists(string file, string relativeTo = null)
        {
            if (!String.IsNullOrEmpty(relativeTo) && (Directory.Exists(relativeTo) || File.Exists(relativeTo)))
            {
                var p = file;
                try
                {
                    file = GetAbsolutePath(file, relativeTo);
                }
                catch
                {
                    file = p;
                }
            }
            return File.Exists(file);
        }

        public static void CopyFolderFast(string source, string target,
            bool recurive = false,
            bool copyOnlyIfNewer = false)
        {
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                string targetFile = Path.Combine(target, Path.GetFileName(file));
                if (copyOnlyIfNewer && File.Exists(targetFile))
                {
                    var sourceInfo = new FileInfo(file);
                    var targetInfo = new FileInfo(targetFile);
                    if (sourceInfo.LastWriteTime > targetInfo.LastWriteTime)
                    {
                        Trace.WriteLine($"Copy: {file} to {targetFile}");
                        File.Copy(file, targetFile, true);
                    }
                }
                else
                {
                    Trace.WriteLine($"Copy: {file} to {targetFile}");
                    File.Copy(file, targetFile, true);
                }
            }
            if (recurive)
            {
                foreach (string dir in Directory.GetDirectories(source))
                {
                    string name = Path.GetFileName(dir);
                    string newTarget = Path.Combine(target, name);
                    CopyFolderFast(dir, newTarget, true, copyOnlyIfNewer);
                }
            }
        }


        /// <summary>
        /// Kopiert einen Ordner per API-Funktion
        /// </summary>
        /// <param name="sourceFolderPath">Pfad zum Quellordner</param>
        /// <param name="destFolderPath">Pfad zum Zielordner</param>
        /// <param name="confirmOverwrites">Gibt an, ob das Überschreiben von 
        /// Ordnern oder Dateien vom Benutzer bestätigt werden soll</param>
        /// <exception cref="IOException">Wird geworfen, wenn der dem Zielordner
        /// übergeordnete Ordner nicht existiert, der Quellordner nicht 
        /// existiert oder beim Kopieren einer der (leider nicht dokumentierten)
        /// Fehler auftritt</exception>
        /// <remarks>
        /// <para>
        /// Bei der Anwendung dieser Methode müssen Sie beachten, 
        /// dass die Angabe des Ziel-Ordners für SHFileOperation der Ordner ist, 
        /// der den zu kopierenden Ordner aufnehmen soll. 
        /// Wenn Sie den Ordner C:\Codebook z.B. in den Ordner G:\Backup 
        /// als Unterordner 'Codebook' kopieren wollen, müssen Sie als Quelle
        /// 'C:\Codebook' und als Ziel 'G:\Backup' angeben.
        /// </para>
        /// </remarks>
        public static void CopyFolder(string sourceFolderPath,
                                      string destFolderPath, bool confirmOverwrites)
        {
            // Überprüfen, ob der Zielordner existiert, 
            // um zum einen das Problem zu vermeiden, dass SHFileOperation beim 
            // Kopieren auf ein nicht existierendes Laufwerk ohne Fehler 
            // ausgeführt wird und zum anderen den Fehler zu vermeiden, den
            // SHFileOperation meldet, wenn dieser Ordner nicht existiert.
            if (Directory.Exists(destFolderPath) == false)
            {
                // Ziel-Parent-Ordner existiert nicht: Ausnahme werfen
                throw new IOException("Der Ziel-Ordner " + destFolderPath +
                                      " existiert nicht");
            }

            // Überprüfen, ob der Quellordner existiert
            if (Directory.Exists(sourceFolderPath) == false)
            {
                throw new IOException("Der Quell-Ordner " + sourceFolderPath +
                                      " existiert nicht");
            }

            try
            {
                // Struktur für die Dateiinformationen erzeugen
                var fileOp = new Shfileopstruct
                {
                    wFunc = foCopy,
                    pFrom = sourceFolderPath + "\x0\x0",
                    pTo = destFolderPath + "\x0\x0"
                };

                // (Unter-)Funktion definieren (ShFileOperation kann auch Dateien und
                // Ordner löschen, verschieben oder umbenennen)

                // Quelle und Ziel definieren. Dabei müssen mehrere Datei- oder
                // Ordnerangaben über 0-Zeichen getrennt werden. Am Ende muss ein
                // zusätzliches 0-Zeichen stehen

                // Flags setzen, sodass ein Rückgängigmachen möglich ist und 
                // dass - je nach Argument confirmOverwrites - keine Nachfrage 
                // beim Überschreiben von Ordnern beim Anwender erfolgt
                if (confirmOverwrites)
                {
                    fileOp.fFlags = fofAllowundo;
                }
                else
                {
                    fileOp.fFlags = fofAllowundo | fofNoconfirmation;
                }

                // ShFileOperation unter Übergabe der Struktur aufrufen
                int result = SHFileOperation(ref fileOp);
                // Erfolg auswerten. SHFileOperation liefert
                // 0 zurück, wenn kein Fehler aufgetreten ist, ansonsten einen
                // (leider undokumentierten) Wert ungleich 0.
                if (result != 0)
                {
                    throw new IOException("Error " + result +
                                          " while copy folder '" +
                                          sourceFolderPath + "' to destination '" +
                                          destFolderPath + "'");
                }
            }
            catch (Exception)
            {
                CopyFolderFast(sourceFolderPath, destFolderPath, true, true);
            }
        }

        /// <summary>
        /// Verschiebt eine Datei
        /// </summary>
        /// <param name="source">Pfad zur Quelldatei bzw. zum Quellordner</param>
        /// <param name="dest">Pfad zum Ziel</param>
        /// <param name="confirmOverwrites">Gibt an, ob das Überschreiben von Dateien,
        /// die im Zielordner bereits vorhanden sind, vom Anwender bestätigt werden soll</param>
        /// <returns>Gibt true zurück wenn das Verschieben erfolgreich war</returns>
        /// <remarks>
        /// <para>
        /// Beim Verschieben von Ordnern verhält sich SHFileOperation etwas gewöhnungsbedürftig: 
        /// Existiert noch kein Ordner mit dem angegebenen Zielpfad, wird der Quellordner in den 
        /// im Zielpfad angegebenen übergeordneten Ordner verschoben und so benannt, 
        /// wie der letzte Ordner im Zielpfad. Das ist das erwartete Verhalten. 
        /// Existiert jedoch bereits ein Ordner mit dem angegebenen Zielpfad, 
        /// wird der Quellordner so in diesem Ordner verschoben, dass er ein Unterordner wird. 
        /// Existiert z.B. ein Ordner C:\Temp\DemoFolder und Sie verschieben den Ordner 
        /// C:\DemoFolder nach C:\Temp\DemoFolder, wird dieser Ordner in den Ordner 
        /// C:\Temp\DemoFolder\DemoFolder verschoben. 
        /// Existiert C:\Temp\DemoFolder beim Verschieben noch nicht, wird der Ordner 
        /// korrekt in den Ordner C:\Temp\DemoFolder verschoben. 
        /// Deshalb sollten Sie beachten, dass Sie beim Verschieben von Ordnern als Ziel 
        /// immer den Pfad zu dem Ordner angeben sollten, in den der verschobene 
        /// Ordner kopiert werden soll (also den Zielpfad zum Parent-Ordner). 
        /// Leider führt das dazu, dass Sie Ordner beim Verschieben nicht umbenennen können. 
        /// </para>
        /// </remarks>
        public static bool CopyFile(string source, string dest,
                                            bool confirmOverwrites)
        {
            // Struktur für die Dateiinformationen erzeugen
            var fileOp = new Shfileopstruct { wFunc = foCopy, pFrom = source + "\x0\x0", pTo = dest + "\x0\x0" };

            // (Unter-)Funktion definieren (ShFileOperation kann auch Dateien und 
            // Ordner löschen und kopieren)

            // Quelle und Ziel definieren. Dabei müssen mehrere Datei- oder
            // Ordnerangaben über 0-Zeichen getrennt werden.
            // Am Ende muss ein zusätzliches 0-Zeichen stehen

            // Flags setzen, sodass ein Rückgängigmachen möglich ist (was aber
            // beim Verschieben zurzeit scheinbar noch nicht unterstützt wird!)
            // und - je nach Argument confirmOverwrites - keine Nachfrage
            // beim Überschreiben von Ordnern beim Anwender erfolgt
            if (confirmOverwrites)
            {
                fileOp.fFlags = fofAllowundo;
            }
            else
            {
                fileOp.fFlags = fofAllowundo | fofNoconfirmation;
            }

            // SHFileOperation unter Übergabe der Struktur aufrufen
            int result = SHFileOperation(ref fileOp);

            // Erfolg oder Misserfolg zurückgeben
            return (result == 0);
        }


        /// <summary>
        /// Verschiebt eine Datei
        /// </summary>
        /// <param name="source">Pfad zur Quelldatei bzw. zum Quellordner</param>
        /// <param name="dest">Pfad zum Ziel</param>
        /// <param name="confirmOverwrites">Gibt an, ob das Überschreiben von Dateien,
        /// die im Zielordner bereits vorhanden sind, vom Anwender bestätigt werden soll</param>
        /// <returns>Gibt true zurück wenn das Verschieben erfolgreich war</returns>
        /// <remarks>
        /// <para>
        /// Beim Verschieben von Ordnern verhält sich SHFileOperation etwas gewöhnungsbedürftig: 
        /// Existiert noch kein Ordner mit dem angegebenen Zielpfad, wird der Quellordner in den 
        /// im Zielpfad angegebenen übergeordneten Ordner verschoben und so benannt, 
        /// wie der letzte Ordner im Zielpfad. Das ist das erwartete Verhalten. 
        /// Existiert jedoch bereits ein Ordner mit dem angegebenen Zielpfad, 
        /// wird der Quellordner so in diesem Ordner verschoben, dass er ein Unterordner wird. 
        /// Existiert z.B. ein Ordner C:\Temp\DemoFolder und Sie verschieben den Ordner 
        /// C:\DemoFolder nach C:\Temp\DemoFolder, wird dieser Ordner in den Ordner 
        /// C:\Temp\DemoFolder\DemoFolder verschoben. 
        /// Existiert C:\Temp\DemoFolder beim Verschieben noch nicht, wird der Ordner 
        /// korrekt in den Ordner C:\Temp\DemoFolder verschoben. 
        /// Deshalb sollten Sie beachten, dass Sie beim Verschieben von Ordnern als Ziel 
        /// immer den Pfad zu dem Ordner angeben sollten, in den der verschobene 
        /// Ordner kopiert werden soll (also den Zielpfad zum Parent-Ordner). 
        /// Leider führt das dazu, dass Sie Ordner beim Verschieben nicht umbenennen können. 
        /// </para>
        /// </remarks>
        public static bool MoveFileOrFolder(string source, string dest,
                                            bool confirmOverwrites)
        {
            // Struktur für die Dateiinformationen erzeugen
            var fileOp = new Shfileopstruct { wFunc = foMove, pFrom = source + "\x0\x0", pTo = dest + "\x0\x0" };

            // (Unter-)Funktion definieren (ShFileOperation kann auch Dateien und 
            // Ordner löschen und kopieren)

            // Quelle und Ziel definieren. Dabei müssen mehrere Datei- oder
            // Ordnerangaben über 0-Zeichen getrennt werden.
            // Am Ende muss ein zusätzliches 0-Zeichen stehen

            // Flags setzen, sodass ein Rückgängigmachen möglich ist (was aber
            // beim Verschieben zurzeit scheinbar noch nicht unterstützt wird!)
            // und - je nach Argument confirmOverwrites - keine Nachfrage
            // beim Überschreiben von Ordnern beim Anwender erfolgt
            if (confirmOverwrites)
            {
                fileOp.fFlags = fofAllowundo;
            }
            else
            {
                fileOp.fFlags = fofAllowundo | fofNoconfirmation;
            }

            // SHFileOperation unter Übergabe der Struktur aufrufen
            int result = SHFileOperation(ref fileOp);

            // Erfolg oder Misserfolg zurückgeben
            return (result == 0);
        }

        /// <summary>
        /// Verschiebt eine Datei in den Papierkorb
        /// </summary>
        /// <param name="path">Pfad zur Datei</param>
        /// <returns>Gibt true wenn das Verschieben erfolgreich war</returns>
        public static bool MoveToRecycleBin(string path)
        {
            // Struktur für die Dateiinformationen erzeugen
            var fileOp = new Shfileopstruct
            {
                pFrom = path + "\x0\x0",
                fFlags = fofAllowundo | fofNoconfirmation,
                wFunc = foDelete
            };

            // Quelle definieren. Dabei müssen mehrere Datei- oder
            // Ordnerangaben über 0-Zeichen getrennt werden.
            // Am Ende muss ein zusätzliches 0-Zeichen stehen

            // Flags setzen, sodass ein Rückgängigmachen möglich ist und
            // keine Nachfrage beim Anwender erfolgt

            // (Unter-)Funktion definieren

            // ShFileOperation unter Übergabe der Struktur aufrufen
            int result = SHFileOperation(ref fileOp);

            // Erfolg oder Fehler zurückmelden. SHFileOperation liefert 0
            // zurück, wenn kein Fehler aufgetreten ist
            return (result == 0);
        }

        /// <summary>
        /// Überprüft, ob eine Pfadangabe gültig ist
        /// </summary>
        /// <param name="path">Der Pfad</param>
        public static bool IsPathValid(string path)
        {
            try
            {
                // GetFullPath aufrufen um eine Exception 
                // bei einem ungültigen Pfad zu provozieren
                Path.GetFullPath(path);
                return true;
            }
            catch (NotSupportedException)
            {
                // Wird bei ungültigen Pfad-Formaten geworfen,
                // z. B. wenn ein Ordner- oder ein Dateiname
                // Doppelpunkte enthält 
                return false;
            }
            catch (ArgumentException)
            {
                // Wird bei ungültigen Zeichen im Pfad geworfen
                return false;
            }
        }

        /// <summary>
        /// Gets registered file types and their associated icon in the system.
        /// </summary>
        /// <returns>Returns a hash table which contains the file extension as keys, the icon file and param as values.</returns>
        public static Hashtable GetFileTypeAndIcon()
        {
            // Create a registry key object to represent the HKEY_CLASSES_ROOT registry section
            RegistryKey rkRoot = Registry.ClassesRoot;

            //Gets all sub keys' names.
            string[] keyNames = rkRoot.GetSubKeyNames();
            var iconsInfo = new Hashtable();

            //Find the file icon.
            foreach (string keyName in keyNames)
            {
                if (String.IsNullOrEmpty(keyName))
                    continue;
                int indexOfPoint = keyName.IndexOf(".");

                //If this key is not a file exttension(eg, .zip), skip it.
                if (indexOfPoint != 0)
                    continue;

                RegistryKey rkFileType = rkRoot.OpenSubKey(keyName);
                if (rkFileType == null)
                    continue;

                //Gets the default value of this key that contains the information of file type.
                object defaultValue = rkFileType.GetValue("");
                if (defaultValue == null)
                    continue;

                //Go to the key that specifies the default icon associates with this file type.
                string defaultIcon = defaultValue + "\\DefaultIcon";
                RegistryKey rkFileIcon = rkRoot.OpenSubKey(defaultIcon);
                if (rkFileIcon != null)
                {
                    //Get the file contains the icon and the index of the icon in that file.
                    object value = rkFileIcon.GetValue("");
                    if (value != null)
                    {
                        //Clear all unecessary " sign in the string to avoid error.
                        string fileParam = value.ToString().Replace("\"", "");
                        iconsInfo.Add(keyName, fileParam);
                    }
                    rkFileIcon.Close();
                }
                rkFileType.Close();
            }
            rkRoot.Close();
            return iconsInfo;
        }

        /// <summary>
        /// Extract the icon from file.
        /// </summary>
        /// <param name="fileAndParam">The params string, 
        /// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
        /// <returns>This method always returns the large size of the icon (may be 32x32 px).</returns>
        public static (EmbeddedIconInfo IconInfo, IntPtr Handle) ExtractIconFromFile(string fileAndParam)
        {
            EmbeddedIconInfo embeddedIcon = GetEmbeddedIconInfo(fileAndParam);

            //Gets the handle of the icon.
            IntPtr lIcon = ExtractIcon(0, embeddedIcon.FileName, embeddedIcon.IconIndex);

            //Gets the real icon.
            return (embeddedIcon, lIcon);
        }
        

        /// <summary>
        /// Parses the parameters string to the structure of EmbeddedIconInfo.
        /// </summary>
        /// <param name="fileAndParam">The params string, 
        /// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
        /// <returns></returns>
        public static EmbeddedIconInfo GetEmbeddedIconInfo(string fileAndParam)
        {
            var embeddedIcon = new EmbeddedIconInfo();

            if (String.IsNullOrEmpty(fileAndParam))
                return embeddedIcon;

            //Use to store the file contains icon.
            string fileName;

            //The index of the icon in the file.
            int iconIndex = 0;
            string iconIndexString = String.Empty;

            int commaIndex = fileAndParam.IndexOf(",");
            //if fileAndParam is some thing likes that: "C:\\Program Files\\NetMeeting\\conf.exe,1".
            if (commaIndex > 0)
            {
                fileName = fileAndParam.Substring(0, commaIndex);
                iconIndexString = fileAndParam.Substring(commaIndex + 1);
            }
            else
                fileName = fileAndParam;

            if (!String.IsNullOrEmpty(iconIndexString))
            {
                //Get the index of icon.
                iconIndex = int.Parse(iconIndexString);
                if (iconIndex < 0)
                    iconIndex = 0;  //To avoid the invalid index.
            }

            embeddedIcon.FileName = fileName;
            embeddedIcon.IconIndex = iconIndex;

            return embeddedIcon;
        }


        /// <summary>
        /// returns Mimetype by given extension
        /// </summary>
        /// <param name="sExtension">The s extension.</param>
        /// <returns></returns>
        public static string GetMimeType(string sExtension)
        {
            string extension = sExtension.ToLower();
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type");
            if (key != null)
                foreach (string keyName in key.GetSubKeyNames())
                {
                    RegistryKey temp = key.OpenSubKey(keyName);
                    if (temp != null)
                        if (extension.Equals(temp.GetValue("Extension")))
                            return keyName;
                }
            return string.Empty;
        }

        public static void TaskCopyDirectory(string src, string dst, bool overwriteExisting = true, bool multiTask = true, bool useWindowsCopy = false, CancellationToken cancellationToken = default)
        {
            if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                return;

            if (Directory.Exists(src))
            {
                if (dst[dst.Length - 1] != Path.DirectorySeparatorChar)
                    dst += Path.DirectorySeparatorChar;
                if (!Directory.Exists(dst))
                    Directory.CreateDirectory(dst);
                string dest = new DirectoryInfo(dst).Parent.FullName;

                if (useWindowsCopy)
                {
                    if (!multiTask)
                    {
                        CopyFolder(src, dest, false);
                    }
                    else
                    {
                        Task.Factory.StartNew(() => CopyFolder(src, dest, false), cancellationToken);
                    }
                }
                else
                {
                    var tasks = new List<Task>();
                    string[] files = Directory.GetFileSystemEntries(src);
                    tasks.AddRange(files.Select(element1 => Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (Directory.Exists(element1))
                                TaskCopyDirectory(element1,
                                              dst + Path.GetFileName(element1),
                                              overwriteExisting, multiTask,
                                              useWindowsCopy, cancellationToken);
                            else
                            {
                                string dstFile = dst + Path.GetFileName(element1);
                                if (overwriteExisting || !File.Exists(dstFile))
                                {
                                    File.Copy(element1, dstFile, true);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }, cancellationToken)));

                    Task.WaitAll(tasks.ToArray());
                }
            }
        }


        /// <summary>
        /// Gets the file description by extension.
        /// </summary>
        /// <param name="sExtension">The s extension.</param>
        /// <param name="extensionFileName"></param>
        /// <returns></returns>
        public static string GetFileDescriptionByExtension(string sExtension, out string extensionFileName)
        {
            if (String.IsNullOrEmpty(sExtension))
            {
                extensionFileName = String.Empty;
                return String.Empty;
            }
            string extension = sExtension.ToLower();
            extensionFileName = extension.Substring(1) + "file";
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension);
            if (key != null)
            {
                object result = key.GetValue(String.Empty);
                if (result != null)
                {
                    string nextKey = result.ToString();
                    extensionFileName = nextKey;
                    result = null;
                    key = Registry.ClassesRoot.OpenSubKey(nextKey);
                    if (key != null) result = key.GetValue(string.Empty);
                    if (result != null)
                    {
                        if (!String.IsNullOrEmpty(result.ToString()))
                            return result.ToString();
                    }
                }
            }

            return sExtension.Substring(1).ToUpper() + "-File";
        }

        /// <summary>
        /// Returns a readable filesize string
        /// </summary>
        public static string GetReadableFileSize(double fileSize, bool fullName = false, Func<string, string> nameConverterFn = null)
        {
            nameConverterFn ??= s => s;
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

            double len = fileSize;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Count)
            {
                order++;
                len = len / 1024;
            }

            var size = sizes.ElementAt(order);
            return $"{len:0.##} {nameConverterFn(fullName ? size.Value : size.Key)}";
        }

    }
    
}