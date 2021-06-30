using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Nextended.Core.Helper
{
	/// <summary>
	/// Helper for Dumpfiles
	/// </summary>
	public static class DumpHelper
	{
		/// <summary>
		/// Create MiniDump
		/// </summary>
		[DllImport("dbghelp.dll")]
		public static extern bool MiniDumpWriteDump(IntPtr hProcess,
													Int32 processId,
													IntPtr hFile,
													int dumpType,
													IntPtr exceptionParam,
													IntPtr userStreamParam,
													IntPtr callackParam);

		/// <summary>
		/// Create MiniDump
		/// </summary>
		public static void CreateDump(string fileName, MinidumpType type)
		{
			if (!fileName.EndsWith(".dmp"))
				fileName += ".dmp";
			using (var fs = new FileStream(fileName, FileMode.Create))
			{
				using (System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess())
				{
					if (fs.SafeFileHandle != null)
					{
						MiniDumpWriteDump(process.Handle,
						                  process.Id,
						                  fs.SafeFileHandle.DangerousGetHandle(),
						                  (int) type,
						                  IntPtr.Zero,
						                  IntPtr.Zero,
						                  IntPtr.Zero);
					}
				}
			}
		}
	}

	/// <summary>
	/// MinidumpType
	/// </summary>
	public enum MinidumpType
	{
		/// <summary>
		/// 
		/// </summary>
		MiniDumpNormal = 0x00000000,

		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithDataSegs = 0x00000001,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithFullMemory = 0x00000002,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithHandleData = 0x00000004,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpFilterMemory = 0x00000008,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpScanMemory = 0x00000010,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithUnloadedModules = 0x00000020,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpFilterModulePaths = 0x00000080,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithProcessThreadData = 0x00000100,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithPrivateReadWriteMemory = 0x00000200,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithoutOptionalData = 0x00000400,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithFullMemoryInfo = 0x00000800,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithThreadInfo = 0x00001000,
		/// <summary>
		/// 
		/// </summary>
		MiniDumpWithCodeSegs = 0x00002000,
	}
}