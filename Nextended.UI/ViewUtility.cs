using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Nextended.Core;
using Nextended.UI.Helper;
using FontStyle = System.Drawing.FontStyle;

namespace Nextended.UI
{
	/// <summary>
	/// Zusammenfassung mehrfach verwendeter Methoden für's GUI.
	/// </summary>
	public static class ViewUtility
	{
		#region Extern DLLImport

		/// <summary>
		/// GetPixel
		/// </summary>
		[DllImport("Gdi32.dll")]
		public static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);

		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

		[DllImport("User32.dll")]
		public static extern void ReleaseDC(IntPtr dc);

		[DllImport("gdi32.dll")]
		private static extern int SetPixel(IntPtr hdc, int x, int y, int color);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		#endregion

		private static readonly Action emptyDelegate = delegate { };

		/// <summary>
		/// Refresh / Rerender given UIElement
		/// </summary>
		public static void Refresh(this UIElement uiElement)
		{
			uiElement.Dispatcher.Invoke(DispatcherPriority.Render, emptyDelegate);
		}

		/// <summary>
		/// Gibt zurück, ob der gesammte Text einer Textbox sichtbar ist.
		/// Ist z.B der Text mit Texttrimming gerade abgeschnitten gibt die funktion false zurück
		/// </summary>
		public static bool IsTextBlockTextVisible(this TextBlock textBlock)
		{
			string font = textBlock.FontFamily.ToString();
			var text = new FormattedText(textBlock.Text,
												CultureInfo.CurrentUICulture,
												 textBlock.FlowDirection, new Typeface(font),
												textBlock.FontSize, textBlock.Foreground);

			var d = textBlock.ActualWidth;
			//var b = VisualTreeHelper.GetContentBounds(textBlock);
			//var w = ((FrameworkElement)textBlock.Parent).ActualWidth;
			if (text.Width > d)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// MeasureStringSize
		/// </summary>
		/// <param name="textToMeasure"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		public static SizeF MeasureStringSize(string textToMeasure, Font font)
		{
			var bmp = new Bitmap(1, 1);
			Graphics graphics = Graphics.FromImage(bmp);
			SizeF size = graphics.MeasureString(textToMeasure, font);
			bmp.Dispose();
			graphics.Dispose();
			return size;
		}

		/// <summary>
		/// Gibt die Farbe eines control an einer bestimmten position zurück
		/// </summary>
		public static System.Drawing.Color GetPixelColor(System.Windows.Forms.Control control, System.Drawing.Point p)
		{
			return GetPixelColor(control, p.X, p.Y);
		}

		/// <summary>
		/// Gibt die Farbe eines control an einer bestimmten position zurück
		/// </summary>
		public static System.Drawing.Color GetPixelColor(System.Windows.Forms.Control control, int x, int y)
		{
			var color = System.Drawing.Color.Empty;
			if (control != null)
			{
				IntPtr hdc = GetDC(control.Handle);
				int colorRef = GetPixel(hdc, x, y);
				color = System.Drawing.Color.FromArgb(
					colorRef & 0x000000FF,
					(colorRef & 0x0000FF00) >> 8,
					(colorRef & 0x00FF0000) >> 16);
				ReleaseDC(control.Handle, hdc);
			}
			return color;
		}

		/// <summary>
		/// Setzt die Farbe eines Controls an einer Position
		/// </summary>
		public static void SetPixelColor(System.Windows.Forms.Control control, int x, int y, System.Drawing.Color color)
		{
			if (control != null)
			{
				IntPtr hdc = GetDC(control.Handle);
				int argb = color.ToArgb();
				int colorRef =
					(argb & 0x00FF0000) >> 16 |
					argb & 0x0000FF00 |
					(argb & 0x000000FF) << 16;
				SetPixel(hdc, x, y, colorRef);
				ReleaseDC(control.Handle, hdc);
			}
		}

		/// <summary>
		/// Konvertiert ein System.Drawing.Icon (Resources .Net 2.0) in einen ImageSource (WPF)
		/// </summary>
		/// <param name="source">Das Bitmap das konvertiert werden soll</param>
		/// <returns></returns>
		public static ImageSource ToIconImageSource(this Icon source)
		{
			var memoryStream = new MemoryStream();
			source.Save(memoryStream);
			memoryStream.Seek(0, SeekOrigin.Begin);
			ImageSource result = BitmapFrame.Create(memoryStream);
			return result;
		}

		/// <summary>
		/// Aus dem Image ein Icon zurückgeben
		/// </summary>
		public static Icon ToIcon(this Bitmap bmp)
		{
			return Icon.FromHandle(bmp.GetHicon());
		}

		/// <summary>
		/// Inverts the specified color.
		/// </summary>
		public static System.Windows.Media.Color Invert(this System.Windows.Media.Color color)
		{
			return System.Windows.Media.Color.FromArgb(color.A, (byte)~color.R, (byte)~color.G, (byte)~color.B);
		}

		/// <summary>
		/// Inverts the specified color.
		/// </summary>
		public static System.Drawing.Color Invert(this System.Drawing.Color color)
		{
			return System.Drawing.Color.FromArgb((byte)~color.R, (byte)~color.G, (byte)~color.B);
		}

		/// <summary>
		/// Media zu Drawing Color
		/// </summary>
		public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color color)
		{
			return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		/// <summary>
		/// Media zu Drawing Color
		/// </summary>
		public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color)
		{
			return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		/// <summary>
		/// Gibt ein normales bild für Disabled zurück
		/// </summary>
		public static System.Drawing.Image ToDisabledGrayscaleImage(this System.Drawing.Image img)
		{
			//create a copy of img,
			// also fix the problem of indexed pixel format (if any)
			var mem = new MemoryStream();
			img.Save(mem, ImageFormat.Png);
			var imag = System.Drawing.Image.FromStream(mem);
			var imge = new Bitmap(imag);

			//create graphics
			Graphics g = Graphics.FromImage(imge);
			//draw disabled image
			ControlPaint.DrawImageDisabled(g, img, 0, 0, System.Drawing.Color.Transparent);

			//retrun result
			return imge;
		}

		/// <summary>
		/// Es wird in VS oder Blend Designer editiert
		/// </summary>
		public static bool InDesignWPF
		{
			get
			{
				if (System.Windows.Application.Current != null)
				{
					if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
						return (bool)System.Windows.Application.Current.Dispatcher.Invoke((Func<bool>)(() => InDesignWPF));

					if (System.Windows.Application.Current.MainWindow != null)
						return DesignerProperties.GetIsInDesignMode(System.Windows.Application.Current.MainWindow);
				}
				return false;
			}
		}

		/// <summary>
		/// Setzt eine Opacity auf ein Image
		/// </summary>
		public static System.Drawing.Image SetOpacity(this System.Drawing.Image image, float opacity)
		{
			var bmpPic = new Bitmap(image.Width, image.Height);
			var gfxPic = Graphics.FromImage(bmpPic);
			var cmxPic = new ColorMatrix { Matrix33 = opacity };

			var iaPic = new ImageAttributes();
			iaPic.SetColorMatrix(cmxPic, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
			gfxPic.DrawImage(image, new Rectangle(0, 0, bmpPic.Width, bmpPic.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, iaPic);
			gfxPic.Dispose();

			return bmpPic;
		}

		/// <summary>
		/// Erzeugt ein eine Bitmapsource aus einem Visual (ideal um z.B ein Image des Splashes zu machen)
		/// </summary>
		public static System.Drawing.Image ToImage(this Visual target, double dpiX = 96, double dpiY = 96)
		{
			var bmpsrc = ToImageSource(target, dpiX, dpiY);
			return ToImage(bmpsrc, ImageFormat.Png);
		}

		/// <summary>
		/// Erzeugt ein eine Bitmapsource aus einem Visual (ideal um z.B ein Image des Splashes zu machen)
		/// </summary>
		public static BitmapSource ToImageSource(this Visual target, double dpiX = 96, double dpiY = 96)
		{
			if (target == null)
			{
				return null;
			}

			Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
			if (bounds == Rect.Empty && target is FrameworkElement)
			{
				return ((FrameworkElement)target).GetBitmapSource(dpiX, dpiY);
			}

			var rtb = new RenderTargetBitmap((int)((bounds.Width+1) * dpiX / 96.0),
															(int)((bounds.Height+1) * dpiY / 96.0),
															dpiX,
															dpiY,
															PixelFormats.Pbgra32);

			var dv = new DrawingVisual();
			using (DrawingContext ctx = dv.RenderOpen())
			{
				var vb = new VisualBrush(target);
				ctx.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), bounds.Size));
			}

			rtb.Render(dv);

			return rtb;
		}

		/// <summary>
		/// Konvert
		/// </summary>
		/// <param name="imageSource"></param>
		public static Bitmap ToBitmap(this ImageSource imageSource)
		{
			using (var stream = new MemoryStream())
			{
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create((BitmapSource)imageSource));
				enc.Save(stream);

				using (var tempBitmap = new Bitmap(stream))
				{
					// According to MSDN, one "must keep the stream open for the lifetime of the Bitmap."
					// So we return a copy of the new bitmap, allowing us to dispose both the bitmap and the stream.
					return new Bitmap(tempBitmap);
				}
			}

		}

		/// <summary>
		/// Finds the resource.
		/// </summary>
		public static object FindResource(object key)
		{
			if (key == null || string.IsNullOrWhiteSpace(key.ToString()))
				return null;
			try
			{
				object result = System.Windows.Application.Current.TryFindResource(key);
				if (result != null)
					return result;
				return FindResourceRecursive(System.Windows.Application.Current.Resources, key);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Sucht eine Resource recursiv
		/// </summary>
		public static object FindResourceRecursive(this ResourceDictionary dictionary, object key)
		{
			if (dictionary.Contains(key))
				return dictionary[key];
			return dictionary.MergedDictionaries.Select(dict => FindResourceRecursive(dict, key)).FirstOrDefault(res => res != null);
		}

		/// <summary>
		/// Sucht alle Resourcen eines typs recursiv
		/// </summary>
		public static IEnumerable<KeyValuePair<object,T>> FindResourceRecursive<T>(this ResourceDictionary dictionary) 
			where T : class
		{
			foreach (DictionaryEntry entry in dictionary)
			{
				var res = dictionary[entry.Key] as T;
				if (res != null)
					yield return new KeyValuePair<object, T>(entry.Key,res);
			}
			foreach (KeyValuePair<object, T> res in dictionary.MergedDictionaries.SelectMany(resourceDictionary => resourceDictionary.FindResourceRecursive<T>()))
			{
				yield return res;
			}
		}

		/// <summary>
		/// Erweiterungsmethode für FrameworkElement
		/// </summary>
		public static BitmapSource GetBitmapSource(this FrameworkElement element, double dpiX = 96, double dpiY = 96)
		{
			var size = new System.Windows.Size(int.MaxValue, int.MaxValue);
			element.Measure(size);
			element.Arrange(new Rect(size));
			element.UpdateLayout();

			var renderBitmap = new RenderTargetBitmap((int)element.DesiredSize.Width + 2,
				   (int)element.DesiredSize.Height + 2, dpiX, dpiY, PixelFormats.Pbgra32);
			renderBitmap.Render(element);
			return renderBitmap;
		}

		/// <summary>
		/// Erweiterungsmethode für FrameworkElement
		/// </summary>
		public static Bitmap GetBitmap(this FrameworkElement element, double dpiX = 96, double dpiY = 96)
		{
			var renderBitmap = element.GetBitmapSource(dpiX, dpiY);
			Bitmap bitmap;
			using (var outStream = new MemoryStream())
			{
				var encoder = new PngBitmapEncoder { Interlace = PngInterlaceOption.On };
				encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
				encoder.Save(outStream);
				bitmap = new Bitmap(outStream);
			}
			return bitmap;
		}


		/// <summary>
		/// Konvertiert ein System.Drawing.Bitmap (Resources .Net 2.0) in einen ImageSource (WPF)
		/// </summary>
		/// <param name="source">Das Bitmap das konvertiert werden soll</param>
		/// <returns></returns>
		public static ImageSource ToImageSource(this Bitmap source)
		{
			if (source == null)
				return null;

			IntPtr hbitmap = source.GetHbitmap();
			BitmapSource bitmap = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());
			DeleteObject(hbitmap);
			return bitmap;
		}


		/// <summary>
		/// Konvertiert ein System.Drawing.Icon (Resources .Net 2.0) in einen ImageSource (WPF)
		/// </summary>
		/// <param name="source">Das Bitmap das konvertiert werden soll</param>
		/// <returns></returns>
		public static ImageSource ToImageSource(this Icon source)
		{
			var memoryStream = new MemoryStream();
			source.Save(memoryStream);
			memoryStream.Seek(0, SeekOrigin.Begin);
			ImageSource result = BitmapFrame.Create(memoryStream);
			return result;
		}

		/// <summary>
		/// Erstellt <see cref="System.Windows.Media.Color"/> aus einem <see cref="uint"/>
		/// </summary>
		/// <param name="argb">ARGB Wert</param>
		public static System.Windows.Media.Color FromUInt(uint argb)
		{
			var color = new System.Windows.Media.Color
							{
								A = (byte)((argb & -16777216) >> 24),
								R = (byte)((argb & 16711680) >> 16),
								G = (byte)((argb & 65280) >> 8),
								B = (byte)(argb & 255)
							};
			return color;
		}

		/// <summary>
		/// Konvertiert WinForms Point in WPF Point
		/// </summary>
		/// <param name="point">WPF point</param>
		public static System.Windows.Point ToPoint(this System.Drawing.Point point)
		{
			return new System.Windows.Point(point.X, point.Y);
		}

		/// <summary>
		/// Gibt das UIElement an der angegebenen Position zurück wenn es dem angegebenem Typen "T" entspricht
		/// </summary>
		public static T GetElementAt<T>(System.Windows.Point position, Visual reference, Func<T, bool> predicate = null) where T : UIElement
		{
			T result = null;
			HitTestParameters p = new PointHitTestParameters(position);
			VisualTreeHelper.HitTest(reference, target =>
			{
				if (target.DependencyObjectType.SystemType.Equals(typeof(T)) && (predicate == null || predicate(target as T)))
				{
					result = target as T;
					return HitTestFilterBehavior.Stop;
				}
				return HitTestFilterBehavior.Continue;
			},
			testResult => HitTestResultBehavior.Continue, p);
			return result;
		}

		/// <summary>
		/// Gibt das UIElement an der angegebenen Position zurück
		/// </summary>
		public static UIElement GetElementAt(System.Windows.Point position, Visual reference)
		{
			var r = VisualTreeHelper.HitTest(reference, position);
			if (r != null && r.VisualHit != null && r.VisualHit is UIElement)
				return (UIElement)r.VisualHit;
			return null;
		}

		/// <summary>
		/// Findet die Position eines elementes
		/// </summary>
		public static System.Windows.Point FindPosition(this Visual control)
		{
			return FindPosition(control, null);
		}

		/// <summary>
		/// Findet die Position eines elementes
		/// </summary>
		public static System.Windows.Point FindPosition(this Visual control, Visual relativeTo)
		{
			var locationToScreen = control.PointToScreen(new System.Windows.Point(0, 0));
			if (relativeTo != null)
				locationToScreen = relativeTo.PointFromScreen(locationToScreen);

			PresentationSource source = PresentationSource.FromVisual(control);
			if (source != null && source.CompositionTarget != null)
				return source.CompositionTarget.TransformFromDevice.Transform(locationToScreen);
			return locationToScreen;
		}

		/// <summary>
		/// Finds the visual parent.
		/// </summary>
		public static TParentItem FindVisualParent<TParentItem>(this DependencyObject obj) where TParentItem : DependencyObject
		{
			DependencyObject current = VisualTreeHelper.GetParent(obj);
			while (current != null && !(current is TParentItem))
				current = VisualTreeHelper.GetParent(current);
			return (TParentItem)current;
		}


		/// <summary>
		/// Sucht den vorfahren des übergebenen WinForm Controls, der dem übergebenem predicate entspricht 
		/// </summary>
		public static System.Windows.Forms.Control FindAncestor(this System.Windows.Forms.Control control, Func<System.Windows.Forms.Control, bool> predicate)
		{
			return FindAncestor<System.Windows.Forms.Control>(control, predicate);
		}

		/// <summary>
		/// Sucht den vorfahren des übergebenen WinForm Controls, welches dem angegebenem Typen und dem übergebenem predicate entspricht  
		/// </summary>
		public static T FindAncestor<T>(this System.Windows.Forms.Control control, Func<T, bool> predicate)
			where T : System.Windows.Forms.Control
		{
			while (control.Parent != null)
			{
				control = control.Parent;
				if (control is T && predicate((T)control))
					return (T)control;
			}
			return null;
		}

		/// <summary>
		/// Sucht den vorfahren des übergebenen WinForm Controls, welches dem angegebenem Typen entspricht 
		/// </summary>
		public static T FindAncestor<T>(this System.Windows.Forms.Control control)
			where T : System.Windows.Forms.Control
		{
			return FindAncestor<T>(control, arg => arg != null);
		}

		/// <summary>
		/// Findet für ein System.Windows.Forms.Control den dazugehörigen WindowsFormsHost
		/// </summary>
		public static WindowsFormsHost FindHost(this System.Windows.Forms.Control control, DependencyObject container = null)
		{
			if (container == null)
			{
				if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null)
					container = System.Windows.Application.Current.MainWindow;
				else
					return null;
			}
			var formsHosts = container.FindDescendants<WindowsFormsHost>();
			if (formsHosts == null)
				return null;
			var list = formsHosts.ToList();
			if (list.Count > 0)
			{
				return (from host in list let r = host.Child.FindDescendants(control1 => control1 == control).FirstOrDefault() where r != null select host).FirstOrDefault();
			}
			return null;
		}

		/// <summary>
		/// Gibt den ersten gefundenen vorfahren von dependencyObject welches vom Typ T ist
		/// </summary>
		public static T FindAncestor<T>(this DependencyObject dependencyObject, Func<T, bool> predicate)
			where T : class
		{
			DependencyObject target = dependencyObject;
			do
			{
				target = VisualTreeHelper.GetParent(target);
				if (target is T && predicate(target as T))
					return target as T;
			}
			while (target != null);
			return null;
		}

		/// <summary>
		/// Gibt den ersten gefundenen vorfahren von dependencyObject welches vom Typ T ist
		/// </summary>
		public static T FindAncestor<T>(this DependencyObject dependencyObject)
			where T : class
		{
			return FindAncestor<T>(dependencyObject, arg => arg != null);
		}

		/// <summary>
		/// Sucht die nachfahren des übergebenen WinForm Controls, welches dem angegebenem Typen und dem übergebenem predicate entspricht  
		/// </summary>
		public static IEnumerable<T> FindDescendants<T>(this System.Windows.Forms.Control control, Func<T, bool> predicate)
			where T : System.Windows.Forms.Control
		{
			var res = new List<T>();
			if (control.HasChildren)
			{
				foreach (var c in control.Controls)
				{
					if (c is T && predicate((T)c))
						res.Add((T)c);
					res.AddRange(FindDescendants((T)c, predicate));
				}
			}
			return res;
		}


		/// <summary>
		/// Sucht die nachfahren des übergebenen WinForm Controls, der dem übergebenem predicate entspricht 
		/// </summary>
		public static IEnumerable<System.Windows.Forms.Control> FindDescendants(this System.Windows.Forms.Control control, Func<System.Windows.Forms.Control, bool> predicate)
		{
			return FindDescendants<System.Windows.Forms.Control>(control, predicate);
		}

		/// <summary>
		/// Sucht die nachfahren des übergebenen WinForm Controls, welches dem angegebenem Typen entspricht 
		/// </summary>
		public static IEnumerable<T> FindDescendants<T>(this System.Windows.Forms.Control control)
			where T : System.Windows.Forms.Control
		{
			return FindDescendants<T>(control, arg => arg != null);
		}

		/// <summary>
		/// Gibt das erste gefundene nachkommen von dependencyObject welches vom Typ T ist
		/// </summary>
		public static T FindDescendant<T>(this DependencyObject dependencyObject, Func<T, bool> predicate)
			 where T : class
		{
			DependencyObject target = dependencyObject;
			var count = VisualTreeHelper.GetChildrenCount(target);
			for (int i = 0; i < count; i++)
			{
				var o = VisualTreeHelper.GetChild(target, i);
				if (o != null && o is T && predicate(o as T))
					return o as T;
				var res = o.FindDescendant(predicate);
				if (res != null)
					return res;
			}
			return null;
		}

		/// <summary>
		/// Gibt das erste gefundene nachkommen von dependencyObject welches vom Typ T ist
		/// </summary>
		public static T FindDescendant<T>(this DependencyObject dependencyObject)
			 where T : class
		{
			return FindDescendant<T>(dependencyObject, arg => arg != null);
		}

		/// <summary>
		/// Liste aller nachfahren von T
		/// </summary>
		public static IEnumerable<T> FindDescendants<T>(this DependencyObject dependencyObject)
			where T : class
		{
			return FindDescendants<T>(dependencyObject, arg => arg != null);
		}

		/// <summary>
		/// Liste aller nachfahren von T
		/// </summary>
		public static IEnumerable<T> FindDescendants<T>(this DependencyObject dependencyObject, Func<T, bool> predicate)
			 where T : class
		{
			DependencyObject target = dependencyObject;
			var result = new List<T>();
			var count = VisualTreeHelper.GetChildrenCount(target);
			for (int i = 0; i < count; i++)
			{
				var o = VisualTreeHelper.GetChild(target, i);
				if (o != null && o is T && predicate(o as T))
					result.Add(o as T);
				result.AddRange(o.FindDescendants(predicate));
			}
			return result;
		}

		/// <summary>
		/// Gibt das UIElement für den übergebenen datacontext in einem itemsControl zurück
		/// </summary>
		public static T GetItemFromDataContext<T>(this ItemsControl itemsControl, object datacontext)
			where T : FrameworkElement
		{
			ItemContainerGenerator generator = itemsControl.ItemContainerGenerator;
			for (int i = 0; i < itemsControl.Items.Count; i++)
			{
				var childControl = generator.ContainerFromIndex(i) as T;
				if (childControl != null && childControl.DataContext == datacontext)
					return childControl;
				if (childControl != null && childControl is ItemsControl && (typeof(T) == typeof(ItemsControl) || typeof(T).IsSubclassOf(typeof(ItemsControl))))
					childControl = GetItemFromDataContext<T>(childControl as ItemsControl, datacontext);
				if (childControl != null) return childControl;
			}
			return null;
		}

		/// <summary>
		/// Konvertiert einen System.Windows.Media.Brush zu einem System.Drawing.Brush
		/// </summary>
		public static System.Drawing.Brush ToDrawingBrush(this System.Windows.Media.Brush brush)
		{
			return brush.ToDrawingBrush(Rectangle.Empty);
		}

		/// <summary>
		/// Konvertiert einen System.Windows.Media.Brush zu einem System.Drawing.Brush
		/// </summary>
		public static System.Drawing.Brush ToDrawingBrush(this System.Windows.Media.Brush brush, Rectangle rectangle)
		{
			if (brush is SolidColorBrush)
				return new SolidBrush(((SolidColorBrush)brush).Color.ToDrawingColor());
			if (brush is GradientBrush)
			{
				var color1 = GetColor(brush, GradientColorOption.First);
				var color2 = GetColor(brush, GradientColorOption.Last);
				return new System.Drawing.Drawing2D.LinearGradientBrush(rectangle, color1, color2, 90f, false);
			}
			return new SolidBrush(GetColor(brush, GradientColorOption.First));
		}

		/// <summary>
		/// Konvertiert einen Brush zu einem ImageSource
		/// </summary>
		public static ImageSource ToImageSource(this System.Windows.Media.Brush brush, int width, int height)
		{
			if (width <= 0) width = 100;
			if (height <= 0) height = 20;
			var drawing = new DrawingVisual();
			DrawingContext context = drawing.RenderOpen();

			context.DrawRectangle(brush, null, new Rect(0, 0, width, height));
			context.Close();

			var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
			bmp.Render(drawing);

			return bmp;

		}

		/// <summary>
		/// Konvertiert einen Brush zu einem Image
		/// </summary>
		public static System.Drawing.Image ToImage(this System.Windows.Media.Brush brush)
		{
			return ToImage(brush, 100, 100);
		}

		/// <summary>
		/// Konvertiert einen Brush zu einem Image
		/// </summary>
		public static System.Drawing.Image ToImage(this System.Windows.Media.Brush brush, int width, int height)
		{
			return ToImage(brush, width, height, ImageFormat.Png);
		}

		/// <summary>
		/// Konvertiert einen Brush zu einem Image
		/// </summary>
		public static System.Drawing.Image ToImage(this System.Windows.Media.Brush brush, int width, int height, ImageFormat format)
		{
			var source = ToImageSource(brush, width, height) as RenderTargetBitmap;
			return source.ToImage(format);
		}


		/// <summary>
		/// Konvertiert einen ImageSource zu einem Image
		/// </summary>
		public static System.Drawing.Image ToImage(this ImageSource imageSource, ImageFormat format = null)
		{
			if (format == null)
				format = ImageFormat.Png;
			var source = imageSource as BitmapSource;
			return source.ToImage(format);
		}

		/// <summary>
		/// Konvertiert ein Bitmapsource in ein Bitmap
		/// </summary>
		/// <param name="bitmapSource">WPF-Bitmapsource das konvertiert werden soll</param>
		/// <param name="format">Bildformat</param>
		public static System.Drawing.Image ToImage(this BitmapSource bitmapSource, ImageFormat format = null)
		{
			if (format == null)
				format = ImageFormat.Png;

			if (bitmapSource != null)
			{
				using (var stream = new MemoryStream())
				{
					BitmapEncoder encoder;
					if (format == ImageFormat.Png)
						encoder = new PngBitmapEncoder();
					else if (format == ImageFormat.Jpeg)
						encoder = new JpegBitmapEncoder();
					else if (format == ImageFormat.Tiff)
						encoder = new TiffBitmapEncoder();
					else if (format == ImageFormat.Gif)
						encoder = new GifBitmapEncoder();
					else
						encoder = new BmpBitmapEncoder();

					encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
					encoder.Save(stream);
					stream.Position = 0;
					var res = new Bitmap(stream);
					stream.Close();
					return res;
				}
			}
			return null;
		}

		/// <summary>
		/// Image To Byte Array
		/// </summary>
		public static byte[] ToByteArray(this System.Drawing.Image image, ImageFormat format)
		{
			var imageStream = new MemoryStream();
			image.Save(imageStream, format);
			imageStream.Flush();
			return imageStream.ToArray();
		}

		///<summary>
		/// Gibt eine liste der gradients von start nach end zurück
		///</summary>
		public static IEnumerable<System.Drawing.Color> GetGradients(System.Drawing.Color start, System.Drawing.Color end, int steps)
		{
			var stepper = System.Drawing.Color.FromArgb((byte)((end.A - start.A) / (steps - 1)),
										   (byte)((end.R - start.R) / (steps - 1)),
										   (byte)((end.G - start.G) / (steps - 1)),
										   (byte)((end.B - start.B) / (steps - 1)));

			for (int i = 0; i < steps; i++)
			{
				yield return System.Drawing.Color.FromArgb(start.A + (stepper.A * i),
											start.R + (stepper.R * i),
											start.G + (stepper.G * i),
											start.B + (stepper.B * i));
			}
		}



		/// <summary>
		/// Konvertiert System.Windos.Media.Color zu System.Drawing.Color
		/// </summary>
		public static System.Drawing.Color GetColor(System.Windows.Media.Color color)
		{
			return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		/// <summary>
		/// Konvertiert Punkte zu Pixeln
		/// </summary>
		/// <param name="points">Punkte</param>
		/// <returns>Pixel</returns>
		public static double PointsToPixels(float points)
		{
			return points * (96.0 / 72.0);
		}

		/// <summary>
		/// Konvertiert Punkte zu Pixeln
		/// </summary>
		/// <param name="pixels">Pixel</param>
		/// <returns>Punkte</returns>
		public static float PixelsToPoints(double pixels)
		{
			return (float)(pixels * (72.0 / 96.0));
		}


		/// <summary>
		/// Konvertiert eine WPF-FontStyle/FontWeight Kombination in ein WinForms FontStyle
		/// </summary>
		/// <param name="style"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public static System.Drawing.FontStyle GetFontStyle(System.Windows.FontStyle style, FontWeight weight)
		{
			var fontStyle = new System.Drawing.FontStyle();

			if (weight.Equals(FontWeights.Bold) || weight.Equals(FontWeights.DemiBold) || weight.Equals(FontWeights.ExtraBold)
				|| weight.Equals(FontWeights.Heavy))
			{
				fontStyle |= System.Drawing.FontStyle.Bold;
			}

			if (style.Equals(FontStyles.Italic))
				fontStyle |= System.Drawing.FontStyle.Italic;
			if (style.Equals(FontStyles.Normal))
				fontStyle |= System.Drawing.FontStyle.Regular;

			return fontStyle;
		}

		/// <summary>
		/// Gibt ein Dictionary mit allen Farben des <paramref name="brush"/> zurück. 
		/// Key ist der offset
		/// </summary>
		public static IEnumerable<KeyValuePair<double, System.Drawing.Color>> GetColors(System.Windows.Media.Brush brush)
		{
			// var result = new Dictionary<double, System.Drawing.Color>();
			var result = new List<KeyValuePair<double, System.Drawing.Color>>();
			if (brush is SolidColorBrush)
				result.Add(new KeyValuePair<double, System.Drawing.Color>(0, ((SolidColorBrush)brush).Color.ToDrawingColor()));
			if (brush is GradientBrush)
				result.AddRange(((GradientBrush)brush).GradientStops.Select(stop => new KeyValuePair<double, System.Drawing.Color>(stop.Offset, stop.Color.ToDrawingColor())));
			return result;
		}

		/// <summary>
		/// Gibt den farbwert eines SolidBrushes, oder den Farbwert des ersten GradientStops 
		/// eines LinearGradientBrushs zurück
		/// </summary>
		public static System.Drawing.Color GetColor(System.Windows.Media.Brush brush, bool lastOnGradient = false)
		{
			return GetColor(brush, lastOnGradient ? GradientColorOption.Last : GradientColorOption.First);
		}

		/// <summary>
		/// Gibt den farbwert eines SolidBrushes, oder den Farbwert des ersten GradientStops 
		/// eines LinearGradientBrushs zurück
		/// </summary>
		public static System.Drawing.Color GetColor(System.Windows.Media.Brush brush, GradientColorOption option)
		{
			if (brush is SolidColorBrush)
				return GetColor(((SolidColorBrush)brush).Color);
			if (brush is GradientBrush)
			{
				var linearGradientBrush = (GradientBrush)brush;
				switch (option)
				{
					case GradientColorOption.First:
						return GetColor(linearGradientBrush.GradientStops.First().Color);
					case GradientColorOption.Last:
						return GetColor(linearGradientBrush.GradientStops.Last().Color);
					case GradientColorOption.MostBrightness:
						return GetColor(GetMostBrightnessColor(linearGradientBrush));
					case GradientColorOption.LeastBrightness:
						return GetColor(GetLeastBrightnessColor(linearGradientBrush));
				}
			}
			return System.Drawing.Color.Transparent;
		}


		/// <summary>
		/// Gibt die hellste farbe aus einem GradientBrush zurück
		/// </summary>
		public static System.Windows.Media.Color GetMostBrightnessColor(GradientBrush brush)
		{
			return GetMostBrightnessColor(brush.GradientStops.Select(gradientStop => gradientStop.Color).ToArray());
		}

		/// <summary>
		/// Gibt die hellste farbe aus einem GradientBrush zurück
		/// </summary>
		public static System.Windows.Media.Color GetLeastBrightnessColor(GradientBrush brush)
		{
			return GetLeastBrightnessColor(brush.GradientStops.Select(gradientStop => gradientStop.Color).ToArray());
		}

		/// <summary>
		/// Gibt die hellste farbe zurück
		/// </summary>
		public static System.Drawing.Color GetMostBrightnessColor(params System.Drawing.Color[] colors)
		{
			return colors.OrderBy(color => color.GetBrightness()).LastOrDefault();
		}

		/// <summary>
		/// Gibt die dunkelste farbe zurück
		/// </summary>
		public static System.Drawing.Color GetLeastBrightnessColor(params System.Drawing.Color[] colors)
		{
			return colors.OrderBy(color => color.GetBrightness()).FirstOrDefault();
		}

		/// <summary>
		/// Gibt die hellste farbe zurück
		/// </summary>
		public static System.Windows.Media.Color GetMostBrightnessColor(params System.Windows.Media.Color[] colors)
		{
			return colors.OrderBy(color => color.ToDrawingColor().GetBrightness()).LastOrDefault();
		}

		/// <summary>
		/// Gibt die dunkelste farbe zurück
		/// </summary>
		public static System.Windows.Media.Color GetLeastBrightnessColor(params System.Windows.Media.Color[] colors)
		{
			return colors.OrderBy(color => color.ToDrawingColor().GetBrightness()).FirstOrDefault();
		}


		/// <summary>
		/// Konvertiert einen WPF System.Windows.Media.LinearGradientBrush
		/// zu einem System.Drawing.Drawing2D.LinearGradientBrush
		/// </summary>
		public static System.Drawing.Drawing2D.LinearGradientBrush ConvertToDrawingLinearGradientBrush(this System.Windows.Media.Brush brush, Rectangle bounds)
		{
			System.Drawing.Color startColor = System.Drawing.Color.Transparent;
			System.Drawing.Color endColor = System.Drawing.Color.Transparent;

			if (brush is LinearGradientBrush)
			{
				startColor = GetColor(((LinearGradientBrush)brush).GradientStops.First().Color);
				endColor = GetColor(((LinearGradientBrush)brush).GradientStops.Last().Color);
			}
			if (brush is SolidColorBrush)
			{
				startColor = GetColor(((SolidColorBrush)brush).Color);
				endColor = GetColor(((SolidColorBrush)brush).Color);
			}

			var brd = new System.Drawing.Drawing2D.LinearGradientBrush(bounds, startColor, endColor, 90);
			return brd;
		}

		/// <summary>
		/// Sets the image alpha.
		/// </summary>
		public static System.Drawing.Image SetImageAlpha(System.Drawing.Image image, float alpha)
		{
			if (!WindowsSecurityHelper.IsWin7OrHigher)
				throw new NotSupportedException("This method not supported on your OS");
			var imgAttr = new ImageAttributes();

			//Standard-ColorMatrix für Transparenz
			var colorMatrix = new ColorMatrix(new[]
                                                  {
		                                            new float[] {1,0,0,0,0},
                                                    new float[] {0,1,0,0,0},
                                                    new float[] {0,0,1,0,0},
                                                    new[] {0,0,0,Convert.ToSingle(alpha / 100),0},
		                                            new float[] {0,0,0,0,1}
                                                  });

			//ColorMatrix an ImageAttribute-Objekt übergeben
			imgAttr.SetColorMatrix(colorMatrix);

			//Neue 32bit Bitmap erstellen
			var newBitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			//Resolution (DPI) vom Quellbitmap auf Zielbitmap übertragen
			newBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			//Graphicsobjekt von NewBitmap erstellen
			Graphics newGraphics = Graphics.FromImage(newBitmap);

			//NewBitmap auf NewGraphics zeichnen
			newGraphics.DrawImage(image, new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imgAttr);

			//Ressource freigeben
			newGraphics.Dispose();
			imgAttr.Dispose();
			return newBitmap;
		}

		/// <summary>
		/// Gibt das IPicture von einem Image zurück, um z.B bilder über COM in delphi zu benutzen
		/// result ist ein IPictureDisp object (stdole)
		/// </summary>
		public static object GetIPicture(System.Drawing.Image image)
		{
			// Reflection, da GetIPictureFromPicture protected static in AxHost ist.
			var methodInfo = typeof(AxHost).GetMethod("GetIPictureFromPicture", BindingFlags.NonPublic | BindingFlags.Static);
			return methodInfo.Invoke(null, new object[] { image });
		}

		/// <summary>
		/// Setzt den Style des übergebenen Controls
		/// </summary>
		public static void SetControlDoubleBuffered(System.Windows.Forms.Control control)
		{
			try
			{
				MethodInfo mi = control.GetType().GetMethod("SetStyle", BindingFlags.NonPublic | BindingFlags.Instance);
				if (mi != null)
				{
					control.Invoke(new Action(() => mi.Invoke(control, new object[]
                                           {
                                               ControlStyles.AllPaintingInWmPaint |
                                               ControlStyles.DoubleBuffer |
                                               ControlStyles.OptimizedDoubleBuffer 
                                                , true
                                           })));

				}
				MethodInfo updateMethod = control.GetType().GetMethod("UpdateStyles", BindingFlags.NonPublic | BindingFlags.Instance);
				if (updateMethod != null)
					control.Invoke(new Action(() => updateMethod.Invoke(control, new object[] { })));
			}
			catch (Exception e)
			{
				Trace.TraceError(e.Message);
			}
		}

		/// <summary>
		/// Ändert die Hintergrundfarbe eines Bildes (Ideal bei Transparenten PNG's)
		/// </summary>
		public static System.Drawing.Image ChangeBackColor(this System.Drawing.Image image, System.Drawing.Color color)
		{
			var objBitmap = new Bitmap(image.Width, image.Height);
			using (Graphics objGfx = Graphics.FromImage(objBitmap))
			{
				objGfx.FillRectangle(new SolidBrush(color), 0, 0, image.Width, image.Height);
				objGfx.DrawImage(image, 0, 0);
			}
			return objBitmap;
		}

		/// <summary>
		/// Gibt die je nach hintergrundfarbe schwarz oder weiß zurück
		/// </summary>
		public static System.Drawing.Color GetOptimalForegroundColor(System.Drawing.Color backgroundColor)
		{
			if (backgroundColor.GetWeightedBrightness() < 48)
				return System.Drawing.Color.White;
			return System.Drawing.Color.Black;
		}

		/// <summary>
		/// Gibt die je nach hintergrundfarbe schwarz oder weiß zurück
		/// </summary>
		public static System.Windows.Media.Color GetOptimalForegroundColor(System.Windows.Media.Color backgroundColor)
		{
			return GetOptimalForegroundColor(backgroundColor.ToDrawingColor()).ToMediaColor();
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
		/// Gibt die tatsächliche hintergrundfarbe des Controls wieder, die der anwender wirklich sieht
		/// </summary>
		public static System.Drawing.Color FindVisualBackground(this System.Windows.Forms.Control control)
		{
			if (control.BackColor != System.Drawing.Color.Empty && control.BackColor != System.Drawing.Color.Transparent)
				return control.BackColor;

			var parent = control.FindAncestor(c => c.BackColor != System.Drawing.Color.Transparent && c.BackColor != System.Drawing.Color.Empty);
			if (parent != null && parent.BackColor != System.Drawing.Color.Empty && parent.BackColor != System.Drawing.Color.Transparent)
				return parent.BackColor;


			var host = control.FindHost();
			if (host != null)
			{
				if (host.Background != null)
				{
					var color = GetColor(host.Background, GradientColorOption.MostBrightness);
					if (color != System.Drawing.Color.Empty && color != System.Drawing.Color.Transparent)
						return color;
				}
				var ancestor = host.FindAncestor<System.Windows.Controls.Control>(c => c.Background != null);
				if (ancestor != null && ancestor.Background != null)
				{
					var color = GetColor(ancestor.Background, GradientColorOption.MostBrightness);
					if (color != System.Drawing.Color.Empty && color != System.Drawing.Color.Transparent)
						return color;
				}
			}

			System.Drawing.Point p = control.Parent.PointToScreen(control.Location);
			IntPtr dc = GetDC(IntPtr.Zero);
			var bg = ColorTranslator.FromWin32(GetPixel(dc, p.X - 1, p.Y - 1));
			if (bg != System.Drawing.Color.Empty && bg != System.Drawing.Color.Transparent)
			{
				return bg;
			}

			return control.BackColor;
		}

		/// <summary>
		/// Gibt die tatsächliche hintergrundfarbe des DependencyObject wieder, die der anwender wirklich sieht
		/// </summary>
		public static System.Windows.Media.Brush FindVisualBackground(this DependencyObject d)
		{
			if (d is System.Windows.Controls.Panel && ((System.Windows.Controls.Panel)d).Background != System.Windows.Media.Brushes.Transparent)
				return ((System.Windows.Controls.Panel)d).Background;

			PropertyInfo propertyInfo = d.GetType().GetProperty("Background");
			if (propertyInfo != null)
			{
				var brush = propertyInfo.GetValue(d, new object[] { }) as System.Windows.Media.Brush;
				if (brush != null && brush != System.Windows.Media.Brushes.Transparent)
					return brush;
			}

			var ancestor = d.FindAncestor<System.Windows.Controls.Control>(c => c.Background != null && c.Background != System.Windows.Media.Brushes.Transparent);
			if (ancestor != null && ancestor.Background != null)
			{
				return ancestor.Background;
			}

			//UIElement
			System.Windows.Media.Brush result = null;
			var parent = d.FindAncestor<UIElement>(c =>
													   {
														   var propInfo = c.GetType().GetProperty("Background");
														   if (propInfo != null)
														   {
															   var brush = propInfo.GetValue(c, new object[] { }) as System.Windows.Media.Brush;
															   if (brush != null && brush != System.Windows.Media.Brushes.Transparent)
															   {
																   result = brush;
																   return true;
															   }
														   }
														   return false;
													   });
			if (parent != null && result != null)
				return result;


			//if (d is UIElement)
			//{
			//    var control = d as UIElement;
			//    System.Windows.Point p = control.FindPosition();
			//    var dp = new System.Drawing.Point(Convert.ToInt32(p.X), Convert.ToInt32(p.Y));
			//    IntPtr dc = GetDC(IntPtr.Zero);
			//    var bg = ColorTranslator.FromWin32(GetPixel(dc, dp.X - 1, dp.Y - 1));
			//    if (bg != System.Drawing.Color.Empty && bg != System.Drawing.Color.Transparent)
			//    {
			//        return new SolidColorBrush(bg.ToMediaColor());
			//    }
			//}

			return null;
		}

		/// <summary>
		/// Konvertiert ein Bild in einen Base64-String
		/// </summary>
		/// <param name="image">
		/// Zu konvertierendes Bild
		/// </param>
		/// <returns>
		/// Base64 Repräsentation des Bildes
		/// </returns>
		public static string ToBase64String(this System.Drawing.Image image)
		{
			if (image != null)
			{
				var ic = new ImageConverter();
				var buffer = (byte[])ic.ConvertTo(image, typeof(byte[]));
				if (buffer != null)
					return Convert.ToBase64String(
						buffer,
						Base64FormattingOptions.InsertLineBreaks);
			}
			return null;

		}
		//---------------------------------------------------------------------
		/// <summary>
		/// Konvertiert einen Base64-String zu einem Bild
		/// </summary>
		/// <param name="base64String">
		/// Zu konvertierender String
		/// </param>
		/// <returns>
		/// Bild das aus dem String erzeugt wird
		/// </returns>
		public static System.Drawing.Image ImageFromBase64String(string base64String)
		{
			byte[] buffer = Convert.FromBase64String(base64String);
			var ic = new ImageConverter();
			return ic.ConvertFrom(buffer) as System.Drawing.Image;
		}


		/// <summary>
		/// ResizeImage
		/// </summary>
		public static System.Drawing.Image ResizeImage(this System.Drawing.Image image, int width, int height)
		{
			// Prevent using images internal thumbnail
			try
			{
				image.RotateFlip(RotateFlipType.Rotate180FlipNone);
				image.RotateFlip(RotateFlipType.Rotate180FlipNone);
			}
			catch { }

			try
			{
				int newHeight = image.Height * width / image.Width;
				if (newHeight > height)
				{
					// Resize with height instead
					width = image.Width * height / image.Height;
					newHeight = height;
				}

				System.Drawing.Image resultImage = image.GetThumbnailImage(width, newHeight, null, IntPtr.Zero);

				// Clear handle to original file so that we can overwrite it if necessary
				image.Dispose();

				return resultImage;
			}
			catch (Exception)
			{
				return image;
			}
		}

		/// <summary>
		/// 2 Bilder mergen
		/// </summary>
		/// <param name="image1"></param>
		/// <param name="image2"></param>
		/// <returns></returns>
		public static Bitmap MergeImages(System.Drawing.Image image1, System.Drawing.Image image2)
		{
			Check.NotNull(() => image1);
			Check.NotNull(() => image2);
			int outputImageWidth = (image1.Width > image2.Width ? image1.Width : image2.Width) + 8;
			int outputImageHeight = (image1.Height > image2.Height ? image1.Height : image2.Height) + image1.Height - 16;

			var outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			using (Graphics graphics = Graphics.FromImage(outputImage))
			{
				graphics.DrawImage(image1, new Rectangle(new System.Drawing.Point(), image1.Size),
					new Rectangle(new System.Drawing.Point(), image1.Size), GraphicsUnit.Pixel);

				graphics.DrawImage(image2, new Rectangle(new System.Drawing.Point(4, image1.Height - 15), image2.Size),
					new Rectangle(new System.Drawing.Point(), image2.Size), GraphicsUnit.Pixel);
			}

			return outputImage;
		}


		/// <summary>
		/// Bilder nebeneinander
		/// </summary>
		/// <param name="image1"></param>
		/// <param name="image2"></param>
		/// <returns></returns>
		public static Bitmap MergeImagesBeside(System.Drawing.Image image1, System.Drawing.Image image2)
		{
			Check.NotNull(() => image1);
			Check.NotNull(() => image2);
			int outputImageWidth = image1.Width + 2 + image2.Width;
			int outputImageHeight = (image1.Height > image2.Height ? image1.Height : image2.Height);

			var outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			using (Graphics graphics = Graphics.FromImage(outputImage))
			{
				graphics.DrawImage(image1, new Rectangle(new System.Drawing.Point(), image1.Size),
					new Rectangle(new System.Drawing.Point(), image1.Size), GraphicsUnit.Pixel);

				graphics.DrawImage(image2, new Rectangle(new System.Drawing.Point(image1.Width + 2, 0), image2.Size),
					new Rectangle(new System.Drawing.Point(), image2.Size), GraphicsUnit.Pixel);
			}

			return outputImage;
		}
		/// <summary>
		/// Schreibt einen Text auf ein Bild
		/// </summary>
		public static System.Drawing.Image WriteText(this System.Drawing.Image image, string text, Font font, System.Drawing.Color color = default(System.Drawing.Color))
		{
			var margin = new System.Windows.Thickness(0);
			if (color == default(System.Drawing.Color))
				color = System.Drawing.Color.Gray;
			return WriteText(image, text, font, color, System.Windows.VerticalAlignment.Center, System.Windows.HorizontalAlignment.Center, margin);
		}

		/// <summary>
		/// Schreibt einen Text auf ein Bild
		/// </summary>
		public static System.Drawing.Image WriteText(this System.Drawing.Image image, string text)
		{
			var font = new Font("Segoe UI", 18, FontStyle.Regular, GraphicsUnit.Point);
			var margin = new System.Windows.Thickness(0);
			return WriteText(image, text, font, System.Drawing.Color.Gray, System.Windows.VerticalAlignment.Center, System.Windows.HorizontalAlignment.Center, margin);
		}

		/// <summary>
		/// Schreibt einen Text auf ein Bild
		/// </summary>
		public static System.Drawing.Image WriteText(this System.Drawing.Image image, string text, System.Drawing.Image additionalImage)
		{
			var font = new Font("Segoe UI", 18, FontStyle.Regular);
			var margin = new System.Windows.Thickness(0);
			return WriteText(image, text, font, System.Drawing.Color.Gray, System.Windows.VerticalAlignment.Center, System.Windows.HorizontalAlignment.Center, margin, additionalImage);
		}

		/// <summary>
		/// Schreibt einen Text auf ein Bild
		/// </summary>
		public static System.Drawing.Image WriteText(this System.Drawing.Image image, string text, Font font, System.Drawing.Color color,
			System.Windows.VerticalAlignment verticalAlignment,
			System.Windows.HorizontalAlignment horizontalAlignment)
		{
			var margin = new System.Windows.Thickness(0);
			return WriteText(image, text, font, System.Drawing.Color.Gray, verticalAlignment, horizontalAlignment, margin);
		}

		/// <summary>
		/// Schreibt einen Text auf ein Bild
		/// </summary>
		public static System.Drawing.Image WriteText(this System.Drawing.Image image, string text, Font font, System.Drawing.Color color,
			System.Windows.VerticalAlignment verticalAlignment,
			System.Windows.HorizontalAlignment horizontalAlignment, System.Windows.Thickness margin, System.Drawing.Image additionalImage = null)
		{

			if (verticalAlignment == System.Windows.VerticalAlignment.Stretch || horizontalAlignment ==  System.Windows.HorizontalAlignment.Stretch)
				throw new NotSupportedException("Alignment 'Stretch' is not Supported ");
			
			var graphicImage = Graphics.FromImage(image);
			graphicImage.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			SizeF textSize = graphicImage.MeasureString(text, font);
			int textHeight = System.Convert.ToInt32(textSize.Height);
			int textWidth = System.Convert.ToInt32(textSize.Width);

			var point = new System.Drawing.Point(0, 0);
			if (verticalAlignment == System.Windows.VerticalAlignment.Center)
				point.Y = (image.Height / 2) - (textHeight / 2);
			if (verticalAlignment == System.Windows.VerticalAlignment.Bottom)
				point.Y = (image.Height) - (textHeight);
			if (horizontalAlignment == System.Windows.HorizontalAlignment.Center)
				point.X = (image.Width / 2) - (textWidth / 2);
			if (horizontalAlignment == System.Windows.HorizontalAlignment.Right)
				point.X = (image.Width) - (textWidth);

			point.X = point.X + System.Convert.ToInt32(margin.Left);
			point.Y = point.Y + System.Convert.ToInt32(margin.Top);


			graphicImage.DrawString(text, font, new SolidBrush(color), point);
			if(additionalImage != null)
			{
				var imagePos = new System.Drawing.Point(point.X - additionalImage.Width - 3, point.Y);
				graphicImage.DrawImage(additionalImage,imagePos);
			}
			return image;
		}

	}

	/// <summary>
	/// GradientColorOption für GetColor
	/// </summary>
	public enum GradientColorOption
	{
		/// <summary>
		/// Die erste Farbe im Gradient
		/// </summary>
		First,

		/// <summary>
		/// Die letzte Farbe im Gradient
		/// </summary>
		Last,

		/// <summary>
		/// Die hellste Farbe im Gradient
		/// </summary>
		MostBrightness,

		/// <summary>
		/// Die dunkelste Farbe im Gradient
		/// </summary>
		LeastBrightness
	}

}
