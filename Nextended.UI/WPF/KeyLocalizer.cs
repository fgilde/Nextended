using System;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using System.Windows.Input;
using Nextended.Core.Properties;
using Application = System.Windows.Application;

namespace Nextended.UI.WPF
{
    /// <summary>
    /// KeyLocalizer
    /// </summary>
    public static class KeyLocalizer
    {
        private static readonly KeyConverter keyConverter = new KeyConverter();
        private static readonly KeysConverter keysConverter = new KeysConverter();

    	/// <summary>
    	/// Übersetzen
    	/// </summary>
    	public static string TranslateKey(string key)
    	{
			if (Application.Current == null || Application.Current.MainWindow == null 
				//||!Application.Current.MainWindow.IsInitialized
				)
				return key;
    		try
    		{
    			var resman = new ResourceManager(typeof(Resources));
    			string result = resman.GetString(key);
    			if (!string.IsNullOrWhiteSpace(result))
    				return result;
    			return key;
    		}
    		catch (Exception)
    		{
    			return key;
    		}
    	}

    	/// <summary>
    	/// Übersetzen
    	/// </summary>
    	public static string TranslateKey(Key key)
    	{
    		return TranslateKey(Enum.GetName(typeof(Key), key));
    	}

    	/// <summary>
    	/// Translates the gesture.
    	/// </summary>
    	public static string TranslateGesture(string gesture)
    	{
    		if (!String.IsNullOrWhiteSpace(gesture))
    		{
    			if (!gesture.Contains("+"))
    				return TranslateKey(gesture);
    			string res = gesture.Split('+').Aggregate(String.Empty, (current, s) => current + (TranslateKey(s) + "+"));
				if (res.EndsWith("+"))
					res = res.Substring(0, res.Length - 1);
				if (res.StartsWith("+"))
					res = res.Substring(1, res.Length);
    			return res;
    		}
    		return gesture;
    	}

    	/// <summary>
        /// Returns Localized displaystring for Keys
        /// </summary>
        public static string GetLocalizedString(params Key[] keys)
        {
            string res = keys.Aggregate(String.Empty, (current, key) => current + ("+" + key.ToLocalizedString()));
            if (res.EndsWith("+"))
                res.Remove(res.Length - 1);
            if (res.StartsWith("+"))
                res = res.Remove(0, 1);
            return res;
        }

		/// <summary>
		/// Gets the localized string for given combined keys.
		/// </summary>
		/// <param name="keys">The keys.</param>
		/// <returns></returns>
        public static string GetLocalizedString(params Keys[] keys)
        {
            string res = keys.Aggregate(String.Empty, (current, key) => current + ("+" + key.ToLocalizedString()));
            if (res.EndsWith("+"))
                res.Remove(res.Length - 1);
            if (res.StartsWith("+"))
                res = res.Remove(0, 1);
            return res;
        }

        /// <summary>
        /// Returns Localized displaystring for Key
        /// </summary>
        public static string ToLocalizedString(this Key key)
        {
            var result = GetTextFromResource(key.ToString());
            return !String.IsNullOrEmpty(result) ? result : keyConverter.ConvertToString(key);
        }

        /// <summary>
        /// Returns Localized displaystring for Key
        /// </summary>
        public static string ToLocalizedString(this Keys key)
        {
            var result = GetTextFromResource(key.ToString());
            return !String.IsNullOrEmpty(result) ? result : keysConverter.ConvertToString(key);
        }

        /// <summary>
        /// Returns Localized displaystring for Key
        /// </summary>
        public static string ToLocalizedString(this KeyGesture gesture)
        {
            return gesture.DisplayString;
        }

		/// <summary>
		/// Gets the text from resource.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns></returns>
        private static string GetTextFromResource(string key)
        {
            try
            {
                var resman = new ResourceManager(typeof(Resources));
                return resman.GetString((key));
            }
            catch 
            {
                return String.Empty;
            }
            
        }
    }
}