using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// WithReference(php) in the AppHost injects the endpoint as services__php__http__0 —
// read it straight from configuration (that's all service discovery would do here too).
var phpBase = app.Configuration["services:php:http:0"]
    ?? throw new InvalidOperationException("No php endpoint injected — run this via Php.AppHost.");
var php = new HttpClient { BaseAddress = new Uri(phpBase) };

app.MapGet("/", () => Results.Content(Page(null), "text/html"));

app.MapPost("/send", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    string result;
    try
    {
        var response = await php.PostAsJsonAsync("/send-mail.php", new
        {
            to = form["to"].ToString(),
            subject = form["subject"].ToString(),
            body = form["body"].ToString(),
        });
        var json = await response.Content.ReadAsStringAsync();
        var cls = response.IsSuccessStatusCode ? "ok" : "err";
        result = $"""<p class="{cls}">PHP answered {(int)response.StatusCode}:</p><pre>{WebUtility.HtmlEncode(json)}</pre>""";
    }
    catch (Exception ex)
    {
        result = $"""<p class="err">Call to PHP failed: {WebUtility.HtmlEncode(ex.Message)}</p>""";
    }
    return Results.Content(Page(result), "text/html");
});

app.Run();

static string Page(string? result) => $$"""
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8">
      <title>Php.WebDemo — send mail via PHP</title>
      <style>
        body { font-family: system-ui, sans-serif; max-width: 40rem; margin: 3rem auto; padding: 0 1rem; }
        label { display: block; margin-top: .8rem; font-weight: 600; }
        input, textarea { width: 100%; padding: .5rem; box-sizing: border-box; }
        button { margin-top: 1rem; padding: .5rem 1.5rem; }
        pre { background: #f4f4f4; padding: .8rem; overflow-x: auto; }
        .ok { color: #0a7d32; } .err { color: #b3261e; }
      </style>
    </head>
    <body>
      <h1>Send mail via PHP 📨</h1>
      <p>This .NET app posts the form to the <code>php</code> Aspire resource
         (<code>send-mail.php</code>), which delivers it over SMTP to Mailpit —
         check the Mailpit dashboard endpoint to see the mail arrive.</p>
      <form method="post" action="/send">
        <label for="to">To</label>
        <input id="to" name="to" type="email" required value="test@example.com">
        <label for="subject">Subject</label>
        <input id="subject" name="subject" value="Hello from .NET via PHP">
        <label for="body">Body</label>
        <textarea id="body" name="body" rows="5">Sent by Php.WebDemo through send-mail.php 🚀</textarea>
        <button type="submit">Send</button>
      </form>
      {{result ?? ""}}
    </body>
    </html>
    """;
