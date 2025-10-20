using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Nextended.Web
{
    public static class WebExtensions
    {
        public static string Action<TController>(this UrlHelper urlHelper, Expression<Action<TController>> actionExpr,
            bool returnAbsolutePath = false)
        {
            var helper = GetHelper(urlHelper?.ActionContext?.HttpContext);
            var res = new Uri(helper.UrlFor(actionExpr));
            return returnAbsolutePath ? res.AbsoluteUri : res.PathAndQuery;
        }

        public static IHtmlContent ActionLink<TController>(this HtmlHelper htmlHelper, string linkName, string protocol, string hostname, string fragment, Expression<Func<TController, object>> actionExpr, object htmlAttributes = null)
        {
            var helper = GetHelper(htmlHelper?.ViewContext?.HttpContext);
            IDictionary<string, object> parameters;
            helper.UrlFor(actionExpr, out parameters);
            var routeValues = new RouteValueDictionary(parameters);
            var actionName = helper.GetActionName(actionExpr);
            var controllerName = helper.GetControllerName<TController>();

            return htmlHelper.ActionLink(linkName, actionName, controllerName, protocol, hostname, fragment, routeValues, htmlAttributes);
        }

        public static IHtmlContent ActionLink<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> actionExpr)
        {
            var helper = GetHelper(htmlHelper?.ViewContext?.HttpContext);
            string urlFor = helper.UrlFor(actionExpr);
            return new HtmlString(string.Format("<a href=\"{1}\" >{0}</a>", urlFor, urlFor));
        }

        public static IHtmlContent ActionLink<TController>(this HtmlHelper htmlHelper, string linkText, Expression<Action<TController>> actionExpr)
        {
            var helper = GetHelper(htmlHelper?.ViewContext?.HttpContext);
            return new HtmlString(string.Format("<a href=\"{1}\" >{0}</a>", linkText, helper.UrlFor(actionExpr)));
        }

        public static RedirectResult RedirectToAction<TController>(this Microsoft.AspNetCore.Mvc.Controller controller, Expression<Action<TController>> actionExpr)
        {
            var helper = GetHelper(controller?.HttpContext);
            return new RedirectResult(helper.UrlFor(actionExpr));
        }

        // ===== interne Helfer =====

        private static RequestHelper GetHelper(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new InvalidOperationException("HttpContext ist nicht verfügbar.");

            return new RequestHelper(GetBaseUrl(httpContext)); // apiPath optional: new RequestHelper(GetBaseUrl(httpContext), "api");
        }

        private static Uri GetBaseUrl(HttpContext httpContext)
        {
            // Request-Basis bestimmen: Scheme + Host (+ PathBase)
            var req = httpContext.Request;
            var builder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);

            // PathBase (z. B. bei Reverse Proxy / Sub-Apps)
            var pathBase = req.PathBase.HasValue ? req.PathBase.Value.Trim('/') : string.Empty;
            if (!string.IsNullOrEmpty(pathBase))
                builder.Path = pathBase + "/";

            return builder.Uri;
        }
    }
}
