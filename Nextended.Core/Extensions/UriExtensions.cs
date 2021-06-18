using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nextended.Core.Extensions
{
	public static class UriExtensions
	{
		public static Uri SetQuery(this Uri uri, object getParams)
		{
			return !uri.IsAbsoluteUri
				? new Uri($"{uri.OriginalString}{getParams.ToQueryString("?")}", UriKind.Relative)
				: new UriBuilder(uri).SetQuery(getParams).Uri;
		}

		public static UriBuilder SetQuery(this UriBuilder builder, object getParams)
		{
			builder.Query = getParams.ToQueryString("?");
			return builder;
		}

		public static Uri Append(this Uri uri, bool shouldEndWithSlash, params string[] segments)
		{
			return new Uri(EnsureSlash(Combine(uri.ToString(), segments), shouldEndWithSlash));
		}

		public static Uri Append(this Uri uri, params string[] segments)
		{
			return new Uri(Combine(uri.ToString(), segments));
		}


		public static string AddParameterToUrl(string url, string key, string value)
		{
			if (!url.Contains(key + "="))
			{
				if (url.Contains("?") || url.Contains("%3F"))
					url += "&" + key + "=" + value;
				else
					url += "?" + key + "=" + value;
			}
			return url;
		}


		public static Uri AddParameter(this Uri uri, string key, string value)
		{
			return new Uri(AddParameterToUrl(uri.AbsoluteUri, key, value));
		}

		public static Uri AddParameter(this Uri uri, IDictionary<string,string> parameters)
		{
			return parameters.Aggregate(uri, (current, p) => new Uri(AddParameterToUrl(current.AbsoluteUri, p.Key, p.Value)));
		}


		public static string Combine(string path, params string[] segments)
		{
			var list = new List<string> { path };
			list.AddRange(segments);
			return Combine(list.ToArray());
		}

		public static string Combine(params string[] segments)
		{
			var sb = new StringBuilder();

			for (int index = 0; index < segments.Length; index++)
			{
				var p = segments[index];
				if (index == segments.Length - 1 && p.Contains("."))
					sb.Append(p);
				else
					sb.Append(EnsureSlash(p));
			}
			return sb.ToString();
		}


		private static string EnsureSlash(string path, bool shouldEndWithSlash = true)
		{
            path = path.EnsureEndsWith("/");
			if (!shouldEndWithSlash)
				path = path.Substring(0, path.Length - 1);
			return path;
		}
	}
}