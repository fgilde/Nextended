using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using Microsoft.AspNetCore.Routing;
using Nextended.Core.Extensions;

namespace Nextended.Web
{
	/// <summary>
	/// Hilfsklasse für reuest und Url behandlungen
	/// </summary>
	public class RequestHelper
	{
		private string apiPath = "api";

		private static readonly MethodInfo DictionaryAdd = ((MethodCallExpression)((Expression<Action<Dictionary<string, object>>>)(d => d.Add(String.Empty, null))).Body).Method;


		/// <summary>
		/// Die Basisadresse des Servers
		/// </summary>
		protected readonly Uri hostUri;


		/// <summary>
		/// WebConfig
		/// </summary>
		protected readonly HttpConfiguration webConfig;


		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Object"/> class.
		/// </summary>		
		public RequestHelper(HttpConfiguration webConfiguration, Uri baseAddress)
		{
			webConfig = webConfiguration;
			//hostUri = webConfiguration.BaseAddress;
			hostUri = baseAddress;
		}


		/// <summary>
		/// Links the specified action expr.
		/// </summary>
		/// <typeparam name="TController">The type of the controller.</typeparam>
		/// <returns></returns>
		/// <exception cref="System.ArgumentException">The expression must be a method call;actionExpr</exception>
		public string UrlFor<TController>(Expression<Action<TController>> actionExpr,
			string routeNameForAttributeRoute = "")			
		{
			IDictionary<string, object> bodyParameters;
			return UrlForCore<TController>((MethodCallExpression)actionExpr.Body, out bodyParameters,
				routeNameForAttributeRoute);
		}


		/// <summary>
		/// Links the specified function expr.
		/// </summary>
		/// <typeparam name="TController">The type of the controller.</typeparam>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="funcExpr">The function expr.</param>
		/// <param name="bodyParameters">The body parameters.</param>
		/// <returns></returns>
		public string UrlFor<TController, TResult>(Expression<Func<TController, TResult>> funcExpr,
			out IDictionary<string, object> bodyParameters)			
		{
			return UrlForCore<TController>((MethodCallExpression)funcExpr.Body, out bodyParameters);
		}


		/// <summary>
		/// Links the specified action name.
		/// </summary>
		/// <typeparam name="TController">The type of the controller.</typeparam>
		/// <param name="actionName">Name of the action.</param>
		/// <param name="routeValuesObject">The route values object.</param>
		/// <returns></returns>
		public string UrlFor<TController>(string actionName = null, object routeValuesObject = null)			
		{
			return BuildUrlWithParameters(BuildBaseUrl<TController>(actionName),
				new RouteValueDictionary(routeValuesObject));
		}


		private string UrlForCore<TController>(MethodCallExpression methodCallExpr,
			out IDictionary<string, object> bodyParameters, string routeNameForAttributeRoute = "")			
		{
			Uri uri = BuildBaseUrl<TController>(GetActionName(methodCallExpr.Method));
			RouteAttribute attributedRouteInfo =
				methodCallExpr.Method.GetCustomAttributes(typeof(RouteAttribute), true)
					.Cast<RouteAttribute>()
					.FirstOrDefault(
						attribute =>
							string.IsNullOrEmpty(routeNameForAttributeRoute) ||
							attribute.Name == routeNameForAttributeRoute);
			if (attributedRouteInfo != null)
				uri = hostUri.Append(attributedRouteInfo.Template);

			IDictionary<string, object> routeParameters;
			PrepareRouteAndBodyParameters(methodCallExpr, out routeParameters, out bodyParameters);
			return BuildUrlWithParameters(uri, routeParameters);
		}


		private Uri BuildBaseUrl<TController>(string actionName)
		{
			bool isApiController = (typeof(TController).IsAssignableFrom(typeof(ApiController)));
			var _hostUri = hostUri;
			if (isApiController)
				_hostUri = _hostUri.Append(GetApiPath<TController>());
			Uri uri = _hostUri.Append(GetControllerName<TController>());
			if (!string.IsNullOrWhiteSpace(actionName))
				uri = uri.Append(actionName);
			return uri;
		}

		private string GetApiPath<TApiController>()
		{
			// TODO: Besser
			return apiPath;
		}


		private string BuildUrlWithParameters(Uri uri, IDictionary<string, object> routeValues)
		{
			string urlstring = uri.ToString();
			foreach (KeyValuePair<string, object> pair in new Dictionary<string, object>(routeValues))
			{
				string findTemplatePattern = "{(" + pair.Key + ".*?)}";
				var match = Regex.Match(urlstring, findTemplatePattern,
					RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline |
					RegexOptions.Singleline);
				if (match.Success)
				{
					string convertToRouteValue = ConvertToRouteValue(pair.Value);
					if (convertToRouteValue == null && match.Value.Contains("?"))
						urlstring = urlstring.Replace(match.Value, string.Empty);
					else if (convertToRouteValue != null)
						urlstring = urlstring.Replace(match.Value, convertToRouteValue);

					if (string.IsNullOrWhiteSpace(convertToRouteValue) && urlstring.Last() == '/')
						urlstring = urlstring.Remove(urlstring.Length - 1);
					routeValues.Remove(pair.Key);
				}
			}
			return
				new Uri(urlstring).AddParameter(routeValues.Where(pair => pair.Value != null)
					.ToDictionary(k => k.Key, e => ConvertToRouteValue(e.Value))).ToString();
		}


		private void PrepareRouteAndBodyParameters(MethodCallExpression methodCallExpr,
			out IDictionary<string, object> routeParameters, out IDictionary<string, object> bodyParameters)
		{
			ParameterInfo[] actionParameters = methodCallExpr.Method.GetParameters();
			IEnumerable<string> bodyParametersNames =
				actionParameters.Where(p => p.GetCustomAttribute<FromBodyAttribute>() != null)
					.Select(p => p.Name)
					.ToList();

			routeParameters = new Dictionary<string, object>();
			bodyParameters = new Dictionary<string, object>();
			if (actionParameters.Any())
			{
				var routeValuesExpr =
					Expression.Lambda<Func<Dictionary<string, object>>>(
						Expression.ListInit(Expression.New(typeof(Dictionary<string, object>)),
							methodCallExpr.Arguments.Select(
								(a, i) =>
									Expression.ElementInit(DictionaryAdd, Expression.Constant(actionParameters[i].Name),
										Expression.Convert(a, typeof(object))))));

				var parameterValueGetter = routeValuesExpr.Compile();
				routeParameters = parameterValueGetter();

				foreach (KeyValuePair<string, object> pair in new Dictionary<string, object>(routeParameters))
				{
					if (bodyParametersNames.Contains(pair.Key))
					{
						bodyParameters.Add(pair);
						routeParameters.Remove(pair.Key);
					}
				}
			}
		}

		public string GetControllerName<TController>()
		{
			Type controllerType = typeof(TController);
			string controllerName = controllerType.Name.Replace("Controller", string.Empty);
			if (webConfig != null)
			{
				Collection<ApiDescription> apiDescriptions = webConfig.Services.GetApiExplorer().ApiDescriptions;
				HttpControllerDescriptor descriptor = apiDescriptions
					.Select(ad => ad.ActionDescriptor.ControllerDescriptor)
					.FirstOrDefault(cd => cd.ControllerType == controllerType);
				if (descriptor != null)
					controllerName = descriptor.ControllerName;
			}
			return controllerName;
		}


		public string GetActionName<TController>(Expression<Action<TController>> actionExpr)
		{
			return GetActionName(((MethodCallExpression)actionExpr.Body).Method);
		}

		public string GetActionName<TController, TResult>(Expression<Func<TController, TResult>> funcExpr)
		{
			return GetActionName(((MethodCallExpression)funcExpr.Body).Method);
		}

		private string GetActionName(MethodInfo methodInfo)
		{
			var result = methodInfo.Name;
			var actionNameAttribute = Attribute.GetCustomAttributes(methodInfo, true).OfType<ActionNameAttribute>().FirstOrDefault();
			if (actionNameAttribute != null)
				result = actionNameAttribute.Name;
			return result;
		}


		private string ConvertToRouteValue(object value)
		{
			if (value != null)
			{
				//if (value is Database)
				//	return ((Database)value).Id.ToString();
				//if (BaseIdFormatter.IsBaseIdType(value.GetType()))
				//	return ((dynamic)value).Id.ToString();
				if (value is Array)
					return String.Join(";", ((Array)value).Cast<object>());
				return value.ToString();
			}
			return null;
		}
	}
}