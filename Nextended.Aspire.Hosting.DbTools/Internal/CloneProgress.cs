using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Nextended.Aspire.Hosting.DbTools;

/// What the database says about itself while it is being filled.
///
/// A clone runs in a container of its own, and its log is where the detail belongs. But the question
/// "is this database ready" is asked at the database, and a database that answers "Running" while it
/// is still empty is answering wrongly. So the clone writes progress markers, and this puts them on
/// the *target* resource, the way Aspire's own long-running steps report themselves:
///
///     hub-dev-copy   Cloning 62% — copying rows (104 of 169 tables)
///
/// The marker is a line the recipes print: <c>##progress 62 copying rows (104 of 169 tables)</c>.
/// Nothing parses prose; a recipe that prints no marker simply shows "Cloning".
internal static class CloneProgress
{
    internal const string Marker = "##progress";

    /// Follows one clone's log and keeps the target's state text in step with it.
    internal static void Follow(
        IDistributedApplicationBuilder builder, IResource target, string cloneResourceName)
    {
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((created, _unused) =>
        {
            var logs = created.Services.GetRequiredService<ResourceLoggerService>();
            var notifications = created.Services.GetRequiredService<ResourceNotificationService>();

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var batch in logs.WatchAsync(cloneResourceName).WithCancellation(CancellationToken.None))
                    {
                        foreach (var line in batch)
                        {
                            var progress = Read(line.Content);
                            if (progress is null) continue;

                            Console.WriteLine($"[dbtools] {target.Name}: {progress}");

                            await notifications.PublishUpdateAsync(target, snapshot => snapshot with
                            {
                                State = new ResourceStateSnapshot(progress, KnownResourceStateStyles.Info),
                            });
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // The stack is going down; the state it had is the state it keeps.
                }
                catch (Exception)
                {
                    // A resource that cannot be watched is not worth taking the app host down for:
                    // the clone's own log still says everything.
                }
            });

            return Task.CompletedTask;
        });
    }

    /// `##progress 62 copying rows (104 of 169 tables)` becomes
    /// `Cloning 62% — copying rows (104 of 169 tables)`, and anything else becomes nothing.
    internal static string? Read(string line)
    {
        var at = line.IndexOf(Marker, StringComparison.Ordinal);
        if (at < 0) return null;

        var rest = line[(at + Marker.Length)..].Trim();
        var space = rest.IndexOf(' ');

        var percentText = space < 0 ? rest : rest[..space];
        var what = space < 0 ? "" : rest[(space + 1)..].Trim();

        if (!int.TryParse(percentText, out var percent)) return null;
        if (percent >= 100) return what.Length > 0 ? what : "Cloned";

        return what.Length > 0 ? $"Cloning {percent}% — {what}" : $"Cloning {percent}%";
    }
}
