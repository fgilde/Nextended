using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Nextended.UI.Input;

/// <summary>Maps raw key names ("D1", "LMenu", "Oem5") to friendly display names ("1", "Left Alt", "Backslash (\\)").</summary>
public static class KeyDisplayName
{
    public static Dictionary<string, string> Overrides { get; } = new()
    {
        { "D1", "1" },
        { "D2", "2" },
        { "D3", "3" },
        { "D4", "4" },
        { "D5", "5" },
        { "D6", "6" },
        { "D7", "7" },
        { "D8", "8" },
        { "D9", "9" },
        { "D0", "0" },

        { "OemMinus", "-" },
        { "OemPlus", "+" },
        { "Back", "Backspace" },
        { "Oem5", "Backslash (\\)" },
        { "LMenu", "Left Alt" },
        { "RMenu", "Right Alt" },
        { "RShiftKey", "Right Shift" },
        { "LShiftKey", "Left Shift" },
        { "LControlKey", "Left Ctrl" },
        { "RControlKey", "Right Ctrl" },
        { "ControlKey", "Ctrl" },

        { "Oem4", "Left Bracket" },
        { "OemOpenBrackets", "Left Bracket" },
        { "Oem6", "Right Bracket" },
        { "Oem1", ";" },
        { "Oem3", "`" },
        { "OemQuotes", "'" },
        { "OemQuestion", "/" },
        { "OemPeriod", "." },
        { "OemComma", "," },
        { "Return", "Enter" },
    };

    public static string For(string? keyName)
    {
        if (string.IsNullOrEmpty(keyName)) return string.Empty;
        if (Overrides.TryGetValue(keyName, out var direct)) return direct;
        try
        {
            var kc = new KeyConverter();
            var key = (Key)kc.ConvertFromString(keyName)!;
            return Overrides.TryGetValue(key.ToString(), out var displayName) ? displayName : key.ToString();
        }
        catch (Exception)
        {
            return keyName;
        }
    }
}
