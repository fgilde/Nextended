using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Nextended.Web
{
	public static class WebExtensions
	{
		public static string Action<TController>(this UrlHelper urlHelper, Expression<Action<TController>> actionExpr, 
			bool returnAbsolutePath = false)
		{
			var res = new Uri(GetHelper().UrlFor(actionExpr));
			return returnAbsolutePath ? res.AbsoluteUri : res.PathAndQuery;
		}

		public static IHtmlContent ActionLink<TController>(this HtmlHelper htmlHelper, string linkName, string protocol, string hostname, string fragment, Expression<Func<TController, object>> actionExpr, object htmlAttributes = null)
		{
			var helper = GetHelper();
			IDictionary<string, object> parameters;
			helper.UrlFor(actionExpr, out parameters);
			RouteValueDictionary routeValues = new RouteValueDictionary(parameters);
            var actionName = helper.GetActionName(actionExpr);
            var controllerName = helper.GetControllerName<TController>();
            
			return htmlHelper.ActionLink(linkName, actionName, controllerName, protocol, hostname, fragment, routeValues, htmlAttributes);
		}

		public static IHtmlContent ActionLink<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> actionExpr)
		{
			string urlFor = GetHelper().UrlFor(actionExpr);
            return new HtmlString(string.Format("<a href=\"{1}\" >{0}</a>", urlFor, urlFor));
		}

		public static IHtmlContent ActionLink<TController>(this HtmlHelper htmlHelper, string linkText, Expression<Action<TController>> actionExpr)
		{
			return new HtmlString(string.Format("<a href=\"{1}\" >{0}</a>", linkText, GetHelper().UrlFor(actionExpr)));
		}

		public static RedirectResult RedirectToAction<TController>(this Controller controller, Expression<Action<TController>> actionExpr)
		{
            // controller.Response.Redirect(GetHelper().UrlFor(actionExpr));
			return new RedirectResult(GetHelper().UrlFor(actionExpr));
		}

		private static Uri GetBaseUrl()
        {
            HttpContext context = new DefaultHttpContext();
			var request = context.Request;
            
			return new Uri(request.GetDisplayUrl());
		}

		private static RequestHelper GetHelper()
		{
			return new RequestHelper(GlobalConfiguration.Configuration, GetBaseUrl());
		}


	}
}