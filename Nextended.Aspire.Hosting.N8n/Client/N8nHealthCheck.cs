using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Nextended.Aspire.Hosting.N8n.Client;

/// <summary>
/// Health check that probes the n8n instance via its <c>/healthz</c> endpoint.
/// </summary>
internal sealed class N8nHealthCheck : IHealthCheck
{
    private readonly N8nApiClient _client;

    public N8nHealthCheck(N8nApiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _client.Http.GetAsync("/healthz", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("n8n is reachable.")
                : HealthCheckResult.Unhealthy($"n8n returned status code {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach n8n.", exception: ex);
        }
    }
}
