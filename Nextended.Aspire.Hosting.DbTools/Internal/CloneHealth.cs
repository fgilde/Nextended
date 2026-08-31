using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Nextended.Aspire.Hosting.DbTools;

/// Whether a clone has finished, for the target to answer with.
///
/// A database resource in Aspire has no state of its own — it reports itself running as soon as its
/// server does, and it cannot be made to wait for anything. Which means a database whose contents are
/// still being copied looks ready, and everything reading it sees an empty database and no reason
/// why. A health check is the one thing a database resource *can* carry, so that is where the answer
/// goes: unhealthy while the clone runs, healthy when it has finished.
///
/// The clone's own resource state is the source of truth, so nothing is written into the copy to
/// track it.
internal sealed class CloneHealth(ResourceNotificationService notifications) : IDisposable
{
    private readonly Dictionary<string, string> states = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource stopping = new();
    private Task? watching;

    /// The states Aspire gives a container that has run to its end. A clone exits 0 both when it
    /// copied and when it found the target already full, and the container's exit code is what tells
    /// those apart from a failure — so "finished" here means "no longer working on it".
    private static readonly string[] Finished = ["Finished", "Exited", "FailedToStart", "RuntimeUnhealthy"];

    internal void Watch()
    {
        watching ??= Task.Run(async () =>
        {
            try
            {
                await foreach (var change in notifications.WatchAsync(stopping.Token))
                {
                    var state = change.Snapshot.State?.Text;
                    if (state is null) continue;

                    lock (states) states[change.Resource.Name] = state;
                }
            }
            catch (OperationCanceledException)
            {
                // The app host is going down; there is nothing left to watch.
            }
        });
    }

    internal HealthCheckResult Ask(string cloneResourceName)
    {
        string? state;
        lock (states) states.TryGetValue(cloneResourceName, out state);

        if (state is null)
            return HealthCheckResult.Unhealthy("the clone has not started yet");

        if (Finished.Contains(state, StringComparer.OrdinalIgnoreCase))
            return HealthCheckResult.Healthy($"the clone {state.ToLowerInvariant()}");

        return HealthCheckResult.Unhealthy($"the clone is still {state.ToLowerInvariant()}");
    }

    public void Dispose()
    {
        stopping.Cancel();
        stopping.Dispose();
    }
}

/// The check the target database carries. One per clone, because each has its own container to wait
/// for.
internal sealed class CloneHealthCheck(CloneHealth health, string cloneResourceName) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(health.Ask(cloneResourceName));
}
