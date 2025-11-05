using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nextended.Core.Helper
{
	public static class EnvironmentHelper
	{
		public static async Task<Dictionary<string, string>> SetEnvironmentVariablesIfNotExistsAsync(IDictionary<string, string> vars, 
			EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
		{
			return await Task.Run(() =>
			{
				var res = new Dictionary<string, string>();
				foreach (var var in vars)
				{
					var current = Environment.GetEnvironmentVariable(var.Key);
					if (string.IsNullOrEmpty(current))
					{
						res.Add(var.Key, var.Value);
						SetEnvironmentVariableWithValueReplace(var.Key, var.Value, target);
					}
				}
				return res;
			});			
		}

		public static async Task<Dictionary<string, string>> SetEnvironmentVariablesIfNotExistsAsync(string key, string value,
			EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
		{
			return await Task.Run(() =>
			{
				var res = new Dictionary<string, string>();				
					var current = Environment.GetEnvironmentVariable(key);
					if (string.IsNullOrEmpty(current))
					{
						res.Add(key, value);
						SetEnvironmentVariableWithValueReplace(key, value, target);
					}				
				return res;
			});
		}

		public static void SetEnvironmentVariables(IDictionary<string, string> vars, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
		{
			foreach (var var in vars)
				SetEnvironmentVariableWithValueReplace(var.Key, var.Value, target);
		}

		public static string ResolveFullValue(string value)
		{
			if (!string.IsNullOrEmpty(value) && value.Contains("%"))
			{
				Regex regex = new Regex(@"\%(.*?)\%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
				MatchCollection matchCollection = regex.Matches(value);
				foreach (Match match in matchCollection)
				{
					var keyValue = Environment.GetEnvironmentVariable(match.Value.Replace("%", string.Empty));
					value = value.Replace(match.Value, keyValue);
				}

			}
			return value;
		}

		public static async Task SetEnvironmentVariableWithValueReplaceAsync(string key, string value,
			EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
		{
			await Task.Run(() => SetEnvironmentVariableWithValueReplace(key, value, target));
		}

		public static void SetEnvironmentVariableWithValueReplace(string key, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process )
		{
			var valueToSet = ResolveFullValue(value);
			Environment.SetEnvironmentVariable(key, valueToSet, target);			
		}

		public static async Task RemoveVariablesAsync(IDictionary<string, string> dict,
			EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
		{
			if (dict != null && dict.Any())
				await RemoveVariablesAsync(dict.Keys, target);
		}

		public static async Task RemoveVariablesAsync(ICollection<string> keys, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
		{			
			//await Task.WhenAll(keys.Select(k => Task.Run(() => Environment.SetEnvironmentVariable(k, null, target))));
			await Task.Run(() =>
			{
				foreach (var key in keys)
					Environment.SetEnvironmentVariable(key, null, target);
			});
		}
	}

	/// <summary>
	/// Provides a scope for temporarily setting environment variables that are automatically restored when disposed.
	/// </summary>
	public class EnvironmentSetScope: IDisposable
	{
		private readonly IDictionary<string, string> varsToSetBack;

		/// <summary>
		/// Initializes a new instance of the EnvironmentSetScope class and sets the specified environment variables.
		/// </summary>
		/// <param name="varsToSet">A dictionary of environment variable names and values to set temporarily.</param>
		public EnvironmentSetScope(IDictionary<string, string> varsToSet)
		{
			if (varsToSet != null)
			{
				varsToSetBack = new Dictionary<string, string>();
				foreach (var var in varsToSet)
				{
					varsToSetBack.Add(new KeyValuePair<string, string>(var.Key, Environment.GetEnvironmentVariable(var.Key)));
					EnvironmentHelper.SetEnvironmentVariableWithValueReplace(var.Key, var.Value);
				}
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			EnvironmentHelper.SetEnvironmentVariables(varsToSetBack);
		}
	}
}