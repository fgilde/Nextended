using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nextended.Imaging
{
	/// <summary>
	/// Miscellaneous
	/// </summary>
	public static class Miscellaneous
	{

		[DllImport(@"urlmon.dll", CharSet = CharSet.Auto)]
		private static extern uint FindMimeFromData(
			uint pBC,
			[MarshalAs(UnmanagedType.LPStr)] string pwzUrl,
			[MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
			uint cbSize,
			[MarshalAs(UnmanagedType.LPStr)] string pwzMimeProposed,
			uint dwMimeFlags,
			out uint ppwzMimeOut,
			uint dwReserverd
		);

		/// <summary>
		/// Farbe zurückgeben
		/// </summary>
		public static Color GetColor(string htmlColor)
		{
			if (string.IsNullOrEmpty(htmlColor))
				return default;
			try
			{
				var c = htmlColor;
				if (htmlColor.Length == 6 && !Enum.GetNames(typeof(KnownColor)).Select(s => s.ToLower()).Contains(htmlColor.ToLower()))
					c = "#FF" + htmlColor;
				return ColorTranslator.FromHtml(c);
			}
			catch (Exception)
			{
				if (!htmlColor.StartsWith("#"))
					return GetColor("#" + htmlColor);
				return Color.FromName(htmlColor);
			}
		}

		/// <summary>
		/// Color to Hex
		/// </summary>
		public static string ToHtml(this Color color)
		{
			return ColorTranslator.ToHtml(color);
		}

		/// <summary>
		/// Gibt die je nach hintergrundfarbe schwarz oder weiß zurück
		/// </summary>
		public static Color GetOptimalForegroundColor(System.Drawing.Color backgroundColor)
		{
			return backgroundColor.GetWeightedBrightness() < 48 ? Color.White : Color.Black;
		}


		/// <summary>
		/// returns a value for the decision whether the text should be black or white
		/// depending on the human eye's sensitivity to the underlying colour
		/// </summary>
		public static int GetWeightedBrightness(this System.Drawing.Color color)
		{
			const double f100 = 1.0 / 7.65; // pure magic
			double r = color.R;
			double g = 1.4 * color.G;
			double b = 0.6 * color.B;
			return (int)Math.Round((r + g + b) * f100);
		}

		/// <summary>
		///     Konvertiert die Farbe zum Integer-Code, gemäß dem
		///     Delphi-Farben z.B. in Reportdefinitionen hinterlegt sind.
		/// </summary>
		/// <param name="foreColor"></param>
		/// <returns></returns>
		public static int ToDelphiColor(this Color foreColor)
		{
			return Color.FromArgb(0, foreColor.B, foreColor.G, foreColor.R).ToArgb();
		}

		/// <summary>
		///     Konvertiert Delphi-Farbcode in WinForms-Color.
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public static Color FromDelphiColor(int color)
		{
			var sysColor = Color.FromArgb(color);
			// Rot und Blau mit Absicht wegen Delphi vertauscht
			return Color.FromArgb(255, sysColor.B, sysColor.G, sysColor.R);
		}


		/// <summary>
		/// MimeSampleSize
		/// </summary>
		public static int MimeSampleSize = 256;

		/// <summary>
		/// DefaultMimeType
		/// </summary>
		public static string DefaultMimeType = "application/octet-stream";


		/// <summary>
		/// Gibt den Mime type der eines byte Arrays zurück
		/// </summary>
		public static string GetMimeFromBytes(byte[] data)
		{
			try
			{
				if (ImageHelper.IsValidImage(data))
					return ImageHelper.GetContentType(data).GetMimeType();

				FindMimeFromData(0, null, data, (uint)MimeSampleSize, null, 0, out var mimeType, 0);
				var mimePointer = new IntPtr(mimeType);
				var mime = Marshal.PtrToStringUni(mimePointer);
				Marshal.FreeCoTaskMem(mimePointer);

				return mime ?? DefaultMimeType;
			}
			catch
			{
				return DefaultMimeType;
			}
		}

        /// <summary>
        /// Gibt den Mime type der Datei zurück
        /// </summary>
        public static string GetMimeType(this FileInfo fileInfo)
        {
            return GetMimeType(fileInfo.Name);
        }

        /// <summary>
        /// /// Gibt den MimeType der Datei zurück
        /// </summary>
        public static string GetMimeType(string fileName)
        {
            return MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(fileName));
        }
    }
}