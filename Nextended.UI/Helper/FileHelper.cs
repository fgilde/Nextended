using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Nextended.UI.Classes;
using FileDialog = System.Windows.Forms.FileDialog;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace Nextended.UI.Helper
{

	public class FileHelper : Core.Helper.FileHelper
	{
        /// <summary>
		/// Gibt das Icon für die übergebene Datei oder extension zurück
		/// </summary>
		public static Icon GetFileIconForExtensionOrFilename(string name)
		{
			Icon result = null;
			if (File.Exists(name))
			{
				try
				{
					result = Icon.ExtractAssociatedIcon(name);
				}
				catch (ArgumentException e)
				{
					//The filePath does not indicate a valid file. Or The filePath indicates a Universal Naming Convention (UNC) path.
					Trace.WriteLine(e);
				}
			}
			else
			{
				string path = Path.GetTempFileName() + "_" + name;
				File.WriteAllBytes(path, new byte[] { 1 });
				if (File.Exists(path))
				{
					try
					{
						result = Icon.ExtractAssociatedIcon(path);
					}
					finally
					{
						File.Delete(path);
					}
				}
			}
			return result;
		}


		/// <summary>
		/// Extract the icon from file.
		/// </summary>
		/// <param name="fileAndParam">The params string, 
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <returns>This method always returns the large size of the icon (may be 32x32 px).</returns>
		public new static Icon ExtractIconFromFile(string fileAndParam)
		{
			EmbeddedIconInfo embeddedIcon = GetEmbeddedIconInfo(fileAndParam);

			//Gets the handle of the icon.
			IntPtr lIcon = ExtractIcon(0, embeddedIcon.FileName, embeddedIcon.IconIndex);

			//Gets the real icon.
			return Icon.FromHandle(lIcon);
		}

		/// <summary>
		/// Extract the icon from file.
		/// </summary>
		/// <param name="fileAndParam">The params string, 
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <param name="isLarge">
		/// Determines the returned icon is a large (may be 32x32 px) 
		/// or small icon (16x16 px).</param>
		public static Icon ExtractIconFromFile(string fileAndParam, bool isLarge)
		{
			var hDummy = new[] { IntPtr.Zero };
			var hIconEx = new[] { IntPtr.Zero };

			try
			{
				EmbeddedIconInfo embeddedIcon = GetEmbeddedIconInfo(fileAndParam);

				uint readIconCount = isLarge ? ExtractIconEx(embeddedIcon.FileName, 0, hIconEx, hDummy, 1) : ExtractIconEx(embeddedIcon.FileName, 0, hDummy, hIconEx, 1);

				if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
				{
					// Get first icon.
					var extractedIcon = (Icon)Icon.FromHandle(hIconEx[0]).Clone();

					return extractedIcon;
				}
				else // No icon read
					return null;
			}
			catch (Exception exc)
			{
				// Extract icon error.
				throw new ApplicationException("Could not extract icon", exc);
			}
			finally
			{
				// Release resources.
				foreach (IntPtr ptr in hIconEx)
					if (ptr != IntPtr.Zero)
						DestroyIcon(ptr);

				foreach (IntPtr ptr in hDummy)
					if (ptr != IntPtr.Zero)
						DestroyIcon(ptr);
			}

		}


		/// <summary>
		/// Ordnerauswahl
		/// </summary>
		public static string BrowseDirectory(string path, string description, bool canCreateFolder)
		{
			var folderSelectionDialog = new FolderBrowserDialog
			{
				SelectedPath = path,
				Description = description,

				ShowNewFolderButton = canCreateFolder
			};

			if (folderSelectionDialog.ShowDialog() == DialogResult.OK)
			{
				return folderSelectionDialog.SelectedPath;
			}
			return string.Empty;
		}

		/// <summary>
		/// Gibt für eine Liste von extension (z.B .txt, .png usw) den filterstring zurück
		/// </summary>
		public static string GetFilterString(IEnumerable<string> extensions)
		{
			IEnumerable<FileDescription> fileDescriptions = extensions.Select(s => new FileDescription(s));
			return fileDescriptions.GetDialogFilter();
		}

		/// <summary>
		/// Dateiauswahl
		/// </summary>
		public static string BrowseFile(params string[] allowedExtensions)
		{
			return BrowseFile("", "", allowedExtensions.First(), GetFilterString(allowedExtensions));
		}

		/// <summary>
		/// Dateiauswahl
		/// </summary>
		public static IEnumerable<string> BrowseFiles(params string[] allowedExtensions)
		{
			return BrowseFiles("", "", allowedExtensions.First(), GetFilterString(allowedExtensions));
		}

		/// <summary>
		/// Dateiauswahl
		/// </summary>
		public static string BrowseFile(string path = "",
										string description = "",
										string defaultExt = "",
										string filter = "",
										bool useSaveDialog = false)
		{
			FileDialog dlg = useSaveDialog
				? (FileDialog)new SaveFileDialog { AutoUpgradeEnabled = true }
				: new OpenFileDialog { CheckFileExists = true, AutoUpgradeEnabled = true };

			if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
				dlg.InitialDirectory = path;
			if (!String.IsNullOrEmpty(description))
				dlg.Title = description;
			if (!String.IsNullOrEmpty(defaultExt))
				dlg.DefaultExt = defaultExt;
			if (!String.IsNullOrEmpty(filter))
				dlg.Filter = filter;

			if (dlg.ShowDialog() == DialogResult.OK)
				return dlg.FileName;

			return string.Empty;
		}

		/// <summary>
		/// Dateiauswahl
		/// </summary>
		public static IEnumerable<string> BrowseFiles(string path = "",
										string description = "",
										string defaultExt = "",
										string filter = "")
		{
			FileDialog dlg = new OpenFileDialog { CheckFileExists = true, AutoUpgradeEnabled = true, Multiselect = true };

			if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
				dlg.InitialDirectory = path;
			if (!string.IsNullOrEmpty(description))
				dlg.Title = description;
			if (!string.IsNullOrEmpty(defaultExt))
				dlg.DefaultExt = defaultExt;
			if (!string.IsNullOrEmpty(filter))
				dlg.Filter = filter;

			if (dlg.ShowDialog() == DialogResult.OK)
				return dlg.FileNames;

			return Enumerable.Empty<string>();
		}
    }
}