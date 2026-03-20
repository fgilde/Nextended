using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Web;

public static class HttpContextExtensions
{
    public static async Task<HttpRequestSnapshot> CaptureAsync(this HttpContext ctx, CancellationToken ct)
    {
        var req = ctx.Request;

        var snap = new HttpRequestSnapshot
        {
            Method = req.Method,
            Scheme = req.Scheme,
            Host = req.Host,
            PathBase = req.PathBase,
            Path = req.Path,
            QueryString = req.QueryString,
            ContentType = req.ContentType,
            ContentLength = req.ContentLength,
            User = ctx.User
        };

        foreach (var h in req.Headers)
            snap.Headers[h.Key] = h.Value;

        req.EnableBuffering();

        if (req.Body.CanRead)
        {
            using var ms = new MemoryStream();
            await req.Body.CopyToAsync(ms, ct);
            snap.Body = ms.ToArray();
            req.Body.Position = 0;
        }

        if (req.HasFormContentType)
        {
            var form = await req.ReadFormAsync(ct);
            snap.Form = form.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
            req.Body.Position = 0;
        }

        return snap;
    }
}

public sealed class HttpRequestSnapshot
{
    public string Method { get; init; } = "GET";
    public string Scheme { get; init; } = "http";
    public HostString Host { get; init; }
    public PathString PathBase { get; init; }
    public PathString Path { get; init; }
    public QueryString QueryString { get; init; }

    public Dictionary<string, StringValues> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public byte[]? Body { get; set; }
    public string? ContentType { get; init; }
    public long? ContentLength { get; init; }

    public Dictionary<string, StringValues>? Form { get; set; }

    public System.Security.Claims.ClaimsPrincipal? User { get; init; }

    public DefaultHttpContext ToDefaultHttpContext(IServiceProvider sp)
    {
        var snap = this;
        var ctx = new DefaultHttpContext
        {
            RequestServices = sp,
            User = snap.User ?? new System.Security.Claims.ClaimsPrincipal()
        };

        var req = ctx.Request;
        req.Method = snap.Method;
        req.Scheme = snap.Scheme;
        req.Host = snap.Host;
        req.PathBase = snap.PathBase;
        req.Path = snap.Path;
        req.QueryString = snap.QueryString;

        req.ContentType = snap.ContentType;
        if (snap.ContentLength.HasValue) req.ContentLength = snap.ContentLength.Value;

        foreach (var h in snap.Headers)
            req.Headers[h.Key] = h.Value;

        if (snap.Body is { Length: > 0 })
        {
            req.Body = new MemoryStream(snap.Body);
            req.Body.Position = 0;
        }

        if (snap.Form is not null)
        {
            ctx.Features.Set<IFormFeature>(new FormFeature(new FormCollection(snap.Form)));
        }

        return ctx;
    }
}