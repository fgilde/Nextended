using System.Windows.Forms;
using Nextended.UI.Classes;

namespace Nextended.UI.Helper
{
	/// <summary>
	/// Debug ausgaben in winform
	/// </summary>
	public static class DebugHelper
	{

		/// <summary>
		/// Ist Debug
		/// </summary>
		public static bool IsDebug
		{

#if DEBUG
			get { return true; }
#else
            get { return false; }
#endif
		}

		/// <summary>
		/// Zeigt den Inhalt eines Objektes an (nur wenn Anwendung im Debugger Läuft (Debugger Attached oder #DEBUG))
		/// </summary>
		public static void DebugOutIfDebug(object obj, string title = "")
		{
			DebugOut(obj, title, System.Diagnostics.Debugger.IsAttached || IsDebug);
		}

		/// <summary>
		/// Zeigt den Inhalt eines Objektes an
		/// </summary>
		public static void DebugOut(object obj, string title = "", bool condition = true)
		{
			if (condition)
			{
				if (obj == null)
					MessageBox.Show("DEBUG: obj is null");
				else
				{
					if (string.IsNullOrWhiteSpace(title))
						title = "DEBUG Info: " + obj;
					var grid = new PropertyGrid { Dock = DockStyle.Fill };
					//grid.SelectedObject = obj;
					grid.SelectedObject = new CustomClass(obj);
					grid.Dock = DockStyle.Fill;
					var window = new Form { Text = title, Height = 400, Width = 400 };
					window.Controls.Add(grid);
					window.Show();
				}
			}
		}
	}
}