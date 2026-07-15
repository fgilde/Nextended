using System.Net;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;
using static Nextended.Aspire.Hosting.N8n.Helpers.N8nLogger;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides extension methods to seed the n8n owner account on startup, so a freshly started
/// instance is immediately usable (login + REST/public API) without the interactive setup screen.
/// </summary>
public static class N8nUserExtensions
{
    /// <summary>
    /// Seeds the n8n <b>owner</b> account on first start by calling <c>POST /rest/owner/setup</c>
    /// once the instance is up. No-op when the instance already has an owner (existing data),
    /// so it is safe to keep this call across restarts. Local run mode only — in publish mode the
    /// owner must be created through n8n's regular setup (the callback never runs there).
    /// </summary>
    /// <remarks>
    /// n8n password policy: at least 8 characters, one number, one capital letter — violations fail
    /// the setup call (logged as warning, n8n keeps running with the interactive setup screen).
    /// </remarks>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="email">Login email of the owner account.</param>
    /// <param name="password">Login password (min 8 chars, 1 number, 1 capital).</param>
    /// <param name="firstName">First name shown in the n8n UI. Default "Admin".</param>
    /// <param name="lastName">Last name shown in the n8n UI. Default "Owner".</param>
    public static IResourceBuilder<N8nResource> WithOwner(
        this IResourceBuilder<N8nResource> builder,
        string email,
        string password,
        string firstName = "Admin",
        string lastName = "Owner")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var resource = builder.Resource;
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            LogWarning($"n8n '{resource.Name}': WithOwner is a run-mode seeding feature and is skipped in publish mode.");
            return builder;
        }

        if (resource.OwnerConfigured)
            return builder;
        resource.OwnerConfigured = true;

        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(resource, async (_, ct) =>
        {
            var baseUrl = resource.HttpEndpoint.Url.TrimEnd('/');
            using var http = new HttpClient();

            // n8n signals readiness only via /healthz — the container being "running" is not enough
            // (DB migrations on first boot can take a while).
            var healthy = false;
            for (var i = 0; i < 150 && !ct.IsCancellationRequested; i++)   // ~5 min
            {
                try
                {
                    var health = await http.GetAsync($"{baseUrl}/healthz", ct).ConfigureAwait(false);
                    if (health.IsSuccessStatusCode) { healthy = true; break; }
                }
                catch (HttpRequestException) { /* not up yet */ }
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
            if (!healthy)
            {
                LogWarning($"n8n '{resource.Name}': instance did not become healthy — owner seeding skipped.");
                return;
            }

            var payload = JsonSerializer.Serialize(new { email, firstName, lastName, password });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{baseUrl}/rest/owner/setup", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                LogInformation($"n8n '{resource.Name}': owner '{email}' created.");
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Typical cause: instance already has an owner (seed survived in the data mount/db).
                LogInformation($"n8n '{resource.Name}': owner already set up — seeding skipped.");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                LogWarning($"n8n '{resource.Name}': owner setup failed ({(int)response.StatusCode}): {body}");
            }
        });

        LogInformation($"n8n '{resource.Name}': owner '{email}' will be seeded on startup.");
        return builder;
    }
}
