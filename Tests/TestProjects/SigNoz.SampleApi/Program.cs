using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// A small service that keeps the SigNoz dashboards busy: HTTP requests (some slow, some failing), database calls,
// outgoing HTTP calls, logs of every level and the .NET runtime metrics. It sends everything over OTLP to
// OTEL_EXPORTER_OTLP_ENDPOINT, which the app host points at the SigNoz collector.
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(o => (o.IncludeFormattedMessage, o.IncludeScopes) = (true, true));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "sample-api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource("Npgsql"))
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddMeter("Npgsql"))
    .UseOtlpExporter();

builder.Services.AddSingleton(NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("db")
    ?? throw new InvalidOperationException("ConnectionStrings:db is missing")));

builder.Services.AddHttpClient("self");
builder.Services.AddHostedService<Traffic>();

var app = builder.Build();

app.MapGet("/", () => "SigNoz sample: /orders, /orders/{id}, /slow, /fail");

app.MapGet("/orders", async (NpgsqlDataSource db, ILogger<Program> log) =>
{
    var count = await Query(db, "SELECT count(*) FROM generate_series(1, 1000)");
    log.LogInformation("Listed {Count} orders", count);
    return Results.Ok(new { count });
});

app.MapGet("/orders/{id:int}", async (int id, NpgsqlDataSource db, ILogger<Program> log) =>
{
    if (id % 7 == 0)
    {
        log.LogWarning("Order {OrderId} not found", id);
        return Results.NotFound();
    }

    // a failing database call for the "failed DB calls" panels
    await Query(db, id % 11 == 0 ? "SELECT * FROM missing_orders" : "SELECT pg_sleep(0.02)");
    return Results.Ok(new { id });
});

app.MapGet("/slow", async () =>
{
    await Task.Delay(Random.Shared.Next(300, 1500));
    return Results.Ok();
});

app.MapGet("/fail", (ILogger<Program> log) =>
{
    var error = new InvalidOperationException("The sample failed on purpose");
    log.LogError(error, "Processing failed");
    throw error;
});

app.Run();

static async Task<long> Query(NpgsqlDataSource db, string sql)
{
    await using var command = db.CreateCommand(sql);
    return await command.ExecuteScalarAsync() is long value ? value : 0;
}

/// <summary>Calls the service's own endpoints a few times a second.</summary>
internal sealed class Traffic(IHttpClientFactory clients, IServer server, ILogger<Traffic> log) : BackgroundService
{
    private static readonly string[] Paths = ["/orders", "/orders", "/orders/3", "/orders/14", "/orders/22", "/slow", "/fail", "/orders/5"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
        var client = clients.CreateClient("self");
        client.BaseAddress = new Uri(address.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost").Replace("+", "localhost"));
        while (!stoppingToken.IsCancellationRequested)
        {
            var path = Paths[Random.Shared.Next(Paths.Length)];
            try
            {
                using var response = await client.GetAsync(path, stoppingToken);
            }
            catch (HttpRequestException e)
            {
                log.LogWarning(e, "Call of {Path} failed", path);
            }

            await Task.Delay(Random.Shared.Next(100, 400), stoppingToken);
        }
    }
}
