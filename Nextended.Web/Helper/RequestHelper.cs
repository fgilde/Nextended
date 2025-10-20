using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;           // ApiControllerAttribute, ActionNameAttribute, RouteAttribute
using Microsoft.AspNetCore.Routing;
using Nextended.Core.Extensions;          // Uri.Append(), Uri.AddParameter()

namespace Nextended.Web
{
    /// <summary>
    /// Hilfsklasse für Request- und URL-Behandlungen (ASP.NET Core only, ohne System.Web)
    /// </summary>
    public class RequestHelper
    {
        private readonly string apiPath;
        protected readonly Uri hostUri;

        private static readonly MethodInfo DictionaryAdd =
            ((MethodCallExpression)((Expression<Action<Dictionary<string, object>>>)(d => d.Add(string.Empty, null))).Body).Method;

        /// <summary>
        /// In ASP.NET Core genügt die Host-Basisadresse.
        /// </summary>
        public RequestHelper(Uri baseAddress, string apiPath = "api")
        {
            hostUri = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
            this.apiPath = apiPath?.Trim('/') ?? "api";
        }

        public string UrlFor<TController>(Expression<Action<TController>> actionExpr, string routeNameForAttributeRoute = "")
        {
            IDictionary<string, object> bodyParameters;
            return UrlForCore<TController>((MethodCallExpression)actionExpr.Body, out bodyParameters, routeNameForAttributeRoute);
        }

        public string UrlFor<TController, TResult>(Expression<Func<TController, TResult>> funcExpr,
            out IDictionary<string, object> bodyParameters)
        {
            return UrlForCore<TController>((MethodCallExpression)funcExpr.Body, out bodyParameters);
        }

        public string UrlFor<TController>(string actionName = null, object routeValuesObject = null)
        {
            return BuildUrlWithParameters(BuildBaseUrl<TController>(actionName), new RouteValueDictionary(routeValuesObject));
        }

        private string UrlForCore<TController>(MethodCallExpression methodCallExpr,
            out IDictionary<string, object> bodyParameters, string routeNameForAttributeRoute = "")
        {
            var actionName = GetActionName(methodCallExpr.Method);
            Uri uri = BuildBaseUrl<TController>(actionName);

            // Attribute-Routings auf der Action berücksichtigen (ASP.NET Core)
            var attributedRouteInfo =
                methodCallExpr.Method
                    .GetCustomAttributes(typeof(RouteAttribute), true)
                    .Cast<RouteAttribute>()
                    .FirstOrDefault(attribute =>
                        string.IsNullOrEmpty(routeNameForAttributeRoute) ||
                        string.Equals(attribute.Name, routeNameForAttributeRoute, StringComparison.Ordinal));

            if (attributedRouteInfo != null && !string.IsNullOrWhiteSpace(attributedRouteInfo.Template))
            {
                // direktes Template verwenden
                uri = hostUri.Append(attributedRouteInfo.Template.TrimStart('/'));
            }

            IDictionary<string, object> routeParameters;
            PrepareRouteAndBodyParameters(methodCallExpr, out routeParameters, out bodyParameters);
            return BuildUrlWithParameters(uri, routeParameters);
        }

        private Uri BuildBaseUrl<TController>(string actionName)
        {
            var controllerType = typeof(TController);

            // Als API werten, wenn ControllerBase abgeleitet oder [ApiController] vorhanden
            bool isApiController =
                typeof(ControllerBase).IsAssignableFrom(controllerType) ||
                controllerType.GetCustomAttribute<ApiControllerAttribute>() != null;

            var baseUri = isApiController ? hostUri.Append(apiPath) : hostUri;

            Uri uri = baseUri.Append(GetControllerName<TController>());
            if (!string.IsNullOrWhiteSpace(actionName))
                uri = uri.Append(actionName);

            return uri;
        }

        private string BuildUrlWithParameters(Uri uri, IDictionary<string, object> routeValues)
        {
            string urlstring = uri.ToString();

            foreach (KeyValuePair<string, object> pair in new Dictionary<string, object>(routeValues))
            {
                string findTemplatePattern = "{(" + pair.Key + ".*?)}";
                var match = Regex.Match(urlstring, findTemplatePattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);

                if (match.Success)
                {
                    string convertToRouteValue = ConvertToRouteValue(pair.Value);

                    if (convertToRouteValue == null && match.Value.Contains("?"))
                        urlstring = urlstring.Replace(match.Value, string.Empty);
                    else if (convertToRouteValue != null)
                        urlstring = urlstring.Replace(match.Value, convertToRouteValue);

                    if (string.IsNullOrWhiteSpace(convertToRouteValue) && urlstring.EndsWith("/"))
                        urlstring = urlstring[..^1];

                    routeValues.Remove(pair.Key);
                }
            }

            // übrige Werte als Query
            return new Uri(urlstring)
                .AddParameter(routeValues.Where(p => p.Value != null)
                .ToDictionary(k => k.Key, v => ConvertToRouteValue(v.Value)))
                .ToString();
        }

        private void PrepareRouteAndBodyParameters(MethodCallExpression methodCallExpr,
            out IDictionary<string, object> routeParameters, out IDictionary<string, object> bodyParameters)
        {
            var actionParameters = methodCallExpr.Method.GetParameters();

            var bodyParameterNames = actionParameters
                .Where(p => p.GetCustomAttribute<FromBodyAttribute>() != null)
                .Select(p => p.Name)
                .ToList();

            routeParameters = new Dictionary<string, object>();
            bodyParameters = new Dictionary<string, object>();

            if (actionParameters.Any())
            {
                var routeValuesExpr =
                    Expression.Lambda<Func<Dictionary<string, object>>>(
                        Expression.ListInit(
                            Expression.New(typeof(Dictionary<string, object>)),
                            methodCallExpr.Arguments.Select(
                                (a, i) => Expression.ElementInit(
                                    DictionaryAdd,
                                    Expression.Constant(actionParameters[i].Name),
                                    Expression.Convert(a, typeof(object))))));

                var parameterValueGetter = routeValuesExpr.Compile();
                routeParameters = parameterValueGetter();

                foreach (var pair in new Dictionary<string, object>(routeParameters))
                {
                    if (bodyParameterNames.Contains(pair.Key))
                    {
                        bodyParameters.Add(pair);
                        routeParameters.Remove(pair.Key);
                    }
                }
            }
        }

        public string GetControllerName<TController>()
        {
            var controllerType = typeof(TController);
            return controllerType.Name.Replace("Controller", string.Empty);
        }

        public string GetActionName<TController>(Expression<Action<TController>> actionExpr)
            => GetActionName(((MethodCallExpression)actionExpr.Body).Method);

        public string GetActionName<TController, TResult>(Expression<Func<TController, TResult>> funcExpr)
            => GetActionName(((MethodCallExpression)funcExpr.Body).Method);

        private string GetActionName(MethodInfo methodInfo)
        {
            var result = methodInfo.Name;

            var actionNameAttribute = Attribute.GetCustomAttributes(methodInfo, true)
                .OfType<ActionNameAttribute>()
                .FirstOrDefault();

            if (actionNameAttribute != null)
                result = actionNameAttribute.Name;

            return result;
        }

        private static string ConvertToRouteValue(object value)
        {
            if (value == null) return null;
            if (value is Array arr) return string.Join(";", arr.Cast<object>());
            return value.ToString();
        }
    }
}
