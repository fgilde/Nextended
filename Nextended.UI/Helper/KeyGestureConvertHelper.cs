using System.Windows.Forms;
using System.Windows.Input;
using Nextended.UI.WPF;

namespace Nextended.UI.Helper
{
    /// <summary>
    /// Statische Hilfklasse, um ein KeyGesture zu konvertieren
    /// </summary>
    public static class KeyGestureConvertHelper
    {
        private static int MOD_ALT = 0x1;
        private static int MOD_CONTROL = 0x2;
        private static int MOD_SHIFT = 0x4;
        private static int MOD_WIN = 0x8;


        /// <summary>
        /// Convertiert einen KeyGesture zu einem String
        /// </summary>
        public static string ConvertToString(this KeyGesture gesture)
        {
            var converter = new KeyGestureConverter();
            return KeyLocalizer.TranslateGesture(converter.ConvertToString(gesture));
        }


        /// <summary>
        /// Convertiert einen KeyGesture zu einem Shortcut
        /// </summary>
        public static T ConvertTo<T>(this KeyGesture gesture) where T : class
        {
            var converter = new KeyGestureConverter();
            return converter.ConvertTo(gesture, typeof(T)) as T;
        }

        /// <summary>
        /// Erstellt einen neuen KeyGesture
        /// </summary>
        public static KeyGesture CreateFrom<T>(T param)
        {
            var converter = new KeyGestureConverter();
            return converter.ConvertFrom(param) as KeyGesture;
        }

        /// <summary>
        /// Erstellt einen neuen KeyGesture
        /// </summary>
        public static KeyGesture CreateFromString(string s)
        {
            return CreateFrom(s);
        }

        /// <summary>
        /// Creates a new KeyGesture from int
        /// </summary>
        public static KeyGesture CreateFromInt(int value)
        {
            var shortcut = (Shortcut) value;
            return CreateFromShortcut(shortcut);
        }

        /// <summary>
        ///  Creates a new KeyGesture from shortcut
        /// </summary>
        public static KeyGesture CreateFromShortcut(Shortcut shortcut)
        {	
            var key = (Keys)shortcut;
            var modifiers = GetModifiers(key);

            Keys keyWithoutModifiers = key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt;

            var resultKey = new KeyConverter().ConvertFrom(keyWithoutModifiers.ToString());
            
            if (resultKey != null)
                return new KeyGesture((Key)resultKey, modifiers);

            return null;
        }


        /// <summary>
        /// Gibt die ModifierKeys aus einem Shortcut zurück
        /// </summary>
        public static ModifierKeys GetModifiers(Shortcut shortcut)
        {
            return GetModifiers((Keys) shortcut);
        }

        /// <summary>
        /// Gibt die ModifierKeys aus Keys zurück
        /// </summary>
        public static ModifierKeys GetModifiers(Keys key)
        {
            int modifiers = 0;

            if ((key & Keys.Alt) == Keys.Alt)
                modifiers = modifiers | MOD_ALT;

            if ((key & Keys.Control) == Keys.Control)
                modifiers = modifiers | MOD_CONTROL;

            if ((key & Keys.Shift) == Keys.Shift)
                modifiers = modifiers | MOD_SHIFT;

            if ((key & Keys.LWin) == Keys.LWin)
                modifiers = modifiers | MOD_WIN;

            return (ModifierKeys) modifiers;
        }
    }
}
