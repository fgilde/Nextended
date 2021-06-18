using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using Nextended.Core;
using Nextended.Core.Helper;
using Nextended.UI.Helper;
using Application = System.Windows.Application;

namespace Nextended.UI.WPF.Extensions
{
	/// <summary>
	/// Helper für Glass
	/// </summary>
	public static class WindowHelper
	{

		/// <summary>
		/// 
		/// </summary>
		public static readonly DependencyProperty SizeProperty =
			DependencyProperty.Register("Size", typeof(Size), typeof(Window), new UIPropertyMetadata(Size.Empty, SizeChangedCallBack));

		private static void SizeChangedCallBack(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			((Window)dependencyObject).Width = ((Size)e.NewValue).Width;
			((Window)dependencyObject).Height = ((Size)e.NewValue).Height;
		}


		/// <summary>
		/// Wird ausgelöst wenn sich <see cref="IsGlassAvailable"/> ändert
		/// </summary>
		public static event EventHandler<EventArgs> IsGlassAvailableChanged;


		/// <summary>
		/// 
		/// </summary>
		public const int Style = -16;
		/// <summary>
		/// 
		/// </summary>
		public const int ExtStyle = -20;
		/// <summary>
		/// 
		/// </summary>
		public const int Maximizebox = 0x10000;
		/// <summary>
		/// 
		/// </summary>
		public const int Minimizebox = 0x20000;
		/// <summary>
		/// 
		/// </summary>
		public const int ExContexthelp = 0x400;
		/// <summary>
		/// 
		/// </summary>
		public const int Syscommand = 0x0112;
		/// <summary>
		/// 
		/// </summary>
		public const int Contexthelp = 0xF180;


		[DllImport("user32.dll")]
		internal static extern int GetWindowLong(IntPtr hWnd, int index);

		[DllImport("user32.dll")]
		internal static extern int SetWindowLong(IntPtr hWnd, int index, int newLong);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		static extern bool DwmIsCompositionEnabled();

		private static bool lastGlassAvailibility;

		static WindowHelper()
		{
			lastGlassAvailibility = IsGlassAvailable;
			SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
		}

		private static void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs userPreferenceChangedEventArgs)
		{
			if (lastGlassAvailibility != IsGlassAvailable)
			{
				lastGlassAvailibility = IsGlassAvailable;
				RaiseIsGlassAvailableChanged();
			}
		}



		/// <summary>
		/// Gets the Window handle.
		/// </summary>
		public static IntPtr GetHandle(this Window window)
		{
			//HwndSource source = (HwndSource)HwndSource.FromVisual(window);
			return new WindowInteropHelper(Application.Current.MainWindow).Handle;
		}

		/// <summary>
		/// Erweitert ein Window mit einem GlassFrame
		/// </summary>
		public static bool ExtendGlassFrame(this Window window)
		{
			return window.ExtendGlassFrame(new Thickness(-1));
		}

		/// <summary>
		/// Erweitert ein Window mit einem GlassFrame
		/// </summary>
		public static bool ExtendGlassFrame(this Window window, Thickness margin)
		{
			if (!DwmIsCompositionEnabled())
				return false;

			IntPtr hwnd = new WindowInteropHelper(window).Handle;
			if (hwnd == IntPtr.Zero)
				throw new InvalidOperationException("The Window must be shown before extending glass.");

			// Set the background to transparent from both the WPF and Win32 perspectives
			window.Background = Brushes.Transparent;
			// ReSharper disable PossibleNullReferenceException
			HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;
			// ReSharper restore PossibleNullReferenceException

			var margins = new Margins(margin);
			DwmExtendFrameIntoClientArea(hwnd, ref margins);
			return true;
		}


		/// <summary>
		/// Gibt zurück ob Glass möglich
		/// </summary>
		public static bool IsGlassAvailable
		{
			get
			{
				return !Environment.CommandLine.Contains("disableaero") &&
					   (WindowsSecurityHelper.IsVistaOrHigher && DwmIsCompositionEnabled());
			}
		}


		/// <summary>
		/// Setzt die Windowbuttons 
		/// </summary>
		public static void SetWindowButtons(this Window window, bool minimizeButtonVisible, bool maximizeButtonVisible)
		{
			IntPtr hWnd = new WindowInteropHelper(window).Handle;
			var winStyle = GetWindowLong(hWnd, Style);

			if (maximizeButtonVisible)
				winStyle |= Maximizebox;
			else
				winStyle &= ~Maximizebox;

			if (minimizeButtonVisible)
				winStyle |= Minimizebox;
			else
				winStyle &= ~Minimizebox;

			SetWindowLong(hWnd, Style, winStyle);
		}

		/// <summary>
		/// Richtet das fenster zentriert zum Owner aus
		/// </summary>
		public static void SetPositionCenteredToOwner(this Window window, Window owner = null)
		{
			if (owner == null)
				owner = window.Owner;
			Check.NotNull(() => owner);
			window.Left = (owner.Left + (owner.Width / 2) - (window.Width / 2));
			window.Top = (owner.Top + (owner.Height / 2) - (window.Height / 2));
		}

		private static void RaiseIsGlassAvailableChanged()
		{
			EventHandler<EventArgs> handler = IsGlassAvailableChanged;
			if (handler != null) handler(null, EventArgs.Empty);
		}

		/// <summary>
		/// Sets the help action for a window
		/// </summary>
		public static void SetHelpAction(this Window window, Action action)
		{
			if (action != null)
			{
				window.PreviewKeyDown += (sender, e) => { if (e.Key == Key.F1) action(); };
				IntPtr hwnd = new WindowInteropHelper(window).Handle;
				int windowLong = GetWindowLong(hwnd, ExtStyle);
				windowLong = windowLong | ExContexthelp;
				SetWindowLong(hwnd, ExtStyle, windowLong);

				var hwndSource = ((HwndSource)PresentationSource.FromVisual(window));
				if (hwndSource != null)
					hwndSource.AddHook((IntPtr ptr, int msg, IntPtr param, IntPtr lParam, ref bool handled) =>
					{
						if (msg == Syscommand && ((int)param & 0xFFF0) == Contexthelp)
						{
							action();
							handled = true;
						}
						return IntPtr.Zero;
					});
			}
		}

		#region Window Flashing API Stuff

		private const UInt32 flashwStop = 0; //Stop flashing. The system restores the window to its original state.
		//private const UInt32 flashwCaption = 1; //Flash the window caption.
		//private const UInt32 flashwTray = 2; //Flash the taskbar button.
		private const UInt32 flashwAll = 3; //Flash both the window caption and taskbar button.
		private const UInt32 flashwTimer = 4; //Flash continuously, until the FLASHW_STOP flag is set.
		//private const UInt32 flashwTimernofg = 12; //Flash continuously until the window comes to the foreground.

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO
		{
			public UInt32 cbSize; //The size of the structure in bytes.
			public IntPtr hwnd; //A Handle to the Window to be Flashed. The window can be either opened or minimized.
			public UInt32 dwFlags; //The Flash Status.
			public UInt32 uCount; // number of times to flash the window
			public UInt32 dwTimeout; //The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		#endregion

		/// <summary>
		/// Fenster blinken lassen
		/// </summary>
		public static void FlashWindow(this Window win, UInt32 count = UInt32.MaxValue)
		{
			//Don't flash if the window is active
			if (win.IsActive) return;

			var h = new WindowInteropHelper(win);

			var info = new FLASHWINFO
			{
				hwnd = h.Handle,
				dwFlags = flashwAll | flashwTimer,
				uCount = count,
				dwTimeout = 0
			};

			info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
			FlashWindowEx(ref info);
		}

		/// <summary>
		/// Blinkendes fenster nich mehr blinken lassen
		/// </summary>
		/// <param name="win"></param>
		public static void StopFlashingWindow(this Window win)
		{
			var h = new WindowInteropHelper(win);

			var info = new FLASHWINFO();
			info.hwnd = h.Handle;
			info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
			info.dwFlags = flashwStop;
			info.uCount = UInt32.MaxValue;
			info.dwTimeout = 0;

			FlashWindowEx(ref info);
		}

		/// <summary>
		/// Fades the dialog out and close.
		/// </summary>
		public static void FadeOutAndClose(this Window dlg)
		{
			if (SystemInformation.TerminalServerSession || SystemHelper.IsVirtualMachine())
			{
				dlg.Close();
			}
			else
			{
				FadeOut(dlg, dlg.Close);
			}
		}

		/// <summary>
		/// Fades the dialog out
		/// </summary>
		public static void FadeOut(this Window dlg, Action completed)
		{
			var animation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(500)));
			if (completed != null)
				animation.Completed += (sender, args) => completed();
			dlg.BeginAnimation(UIElement.OpacityProperty, animation);
		}


		/// <summary>
		/// Fades the dialog out and close.
		/// </summary>
		public static void FadeInAndShow(this Window dlg, bool modal)
		{
			if (SystemInformation.TerminalServerSession || SystemHelper.IsVirtualMachine())
			{
				dlg.Show(modal);
			}
			else
			{
				dlg.Opacity = 0;
				dlg.Show(modal);
				FadeIn(dlg);
			}
		}

		/// <summary>
		/// Shows the specified window.
		/// </summary>
		public static bool? Show(this Window window, bool modal)
		{
			if (modal)
				return window.ShowDialog();
			window.Show();
			return null;
		}

		/// <summary>
		/// Fades the dialog in.
		/// </summary>
		public static void FadeIn(this Window dlg)
		{
			var animation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(500)));
			dlg.BeginAnimation(UIElement.OpacityProperty, animation);
		}

		/// <summary>
		/// Gets the actual size.
		/// </summary>
		public static Size GetActualSize(this Window window)
		{
			return new Size(window.ActualWidth, window.ActualHeight);
		}

		/// <summary>
		/// Changes the dialog size with animation.
		/// </summary>
		public static void ChangeDialogSizeWithAnimation(this Window window, Size newSize,
			Action completed = null)
		{
			window.SetValue(SizeProperty, new Size(window.Width, window.Height));
			var differenceLeft = newSize.Width - window.Width;
			var differenceTop = newSize.Height - window.Height;
			var duration = new Duration(TimeSpan.FromSeconds(0.3));
			var sizeAnimation = new SizeAnimation(newSize, duration);
			if (completed != null)
				sizeAnimation.Completed += (sender, args) => completed();

			var left = window.Left - differenceLeft / 2;
			var top = window.Top - differenceTop / 2;
			var positionAnimationLeft = new DoubleAnimation(left, duration);
			var positionAnimationTop = new DoubleAnimation(top, duration);

			window.BeginAnimation(SizeProperty, sizeAnimation);
			window.BeginAnimation(Window.LeftProperty, positionAnimationLeft);
			window.BeginAnimation(Window.TopProperty, positionAnimationTop);
		}

		/// <summary>
		/// Gibt zurück ob das aktuelle fenster gerade modal ist
		/// </summary>
		/// <param name="window"></param>
		/// <returns></returns>
		public static bool IsModal(this Window window)
		{
			//return ComponentDispatcher.IsThreadModal;
			try
			{
				return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
			}
			catch (Exception)
			{
				return IsAnyModalDialogOpen();
			}
		}

		/// <summary>
		/// Gibt an ob ein Modaler dialog auf ist
		/// </summary>
		public static bool IsAnyModalDialogOpen()
		{
			return ComponentDispatcher.IsThreadModal;
		}

		/// <summary>
		/// Wechselt von dem senderwindow per size animation zu dem windowToOpen
		/// </summary>
		public static bool? MorphTransitionTo(this Window senderWindow, Window dialog)
		{
			var owner = senderWindow.Owner;
			bool shouldBeModal = senderWindow.IsModal();
			senderWindow.SizeToContent = SizeToContent.Manual;
			senderWindow.Topmost = true;

			dialog.WindowStartupLocation = WindowStartupLocation.Manual;
			dialog.Left = -10000;
			dialog.Top = -10000;
			dialog.SourceInitialized += (sender, args) =>
			{

				//ComponentDispatcher.
				var desiredSize = dialog.DesiredSize;
				dialog.Visibility = Visibility.Hidden;
				senderWindow.ChangeDialogSizeWithAnimation(desiredSize, () =>
				{

					dialog.Left = senderWindow.Left;
					dialog.Top = senderWindow.Top;
					dialog.Visibility = Visibility.Visible;
					dialog.Topmost = true;

					senderWindow.Dispatcher.DoEvents(DispatcherPriority.Send);
					senderWindow.Visibility = Visibility.Hidden;
					//senderWindow.Close();
					dialog.Owner = owner;
				});
			};
			// TODO: aktuell ist der dialog leider nie modal, da dass senderwindow geschlossen wird
			return dialog.Show(shouldBeModal);
		}


	}

	struct Margins
	{
		public Margins(Thickness t)
		{
			Left = (int)t.Left;
			Right = (int)t.Right;
			Top = (int)t.Top;
			Bottom = (int)t.Bottom;
		}
		public int Left;
		public int Right;
		public int Top;
		public int Bottom;
	}
}