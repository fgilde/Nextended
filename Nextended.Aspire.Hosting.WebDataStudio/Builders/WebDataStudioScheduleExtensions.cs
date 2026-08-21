using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// A query the studio runs on a schedule and writes to a file — the nightly report nobody wants to
/// remember to run.
/// </summary>
/// <param name="Name">Names the job, and the files it writes.</param>
/// <param name="Connection">Connection name, as the studio shows it.</param>
/// <param name="Sql">One reading statement. A write is refused when it runs.</param>
/// <param name="EveryMinutes">Run this often. Use this or <paramref name="DailyAtUtc"/>.</param>
/// <param name="DailyAtUtc">Run once a day at this time in UTC, e.g. <c>03:00</c>.</param>
/// <param name="Format">Export format: <c>csv</c> (the default), <c>json</c>, <c>xlsx</c>, …</param>
/// <param name="MaxRows">Row cap for the run.</param>
public sealed record ScheduledStudioQuery(
    string Name, string Connection, string Sql, int? EveryMinutes = null, string? DailyAtUtc = null,
    string? Format = null, int? MaxRows = null);

/// <summary>
/// Scheduled queries, written as a file the studio reads. The file is generated into the app host's
/// output and mounted read-only, so the schedule lives in your app host next to everything else
/// rather than in a volume somebody has to remember.
/// </summary>
public static class WebDataStudioScheduleExtensions
{
    private const string ScheduleTarget = "/data/schedule/schedule.json";
    private const string OutputTarget = "/data/exports";

    /// <summary>
    /// Runs these queries on a schedule and writes each result as a file. Chaining adds jobs.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="jobs">The queries to run.</param>
    /// <remarks>
    /// Only reading statements run: a schedule cannot become a way to run a <c>DELETE</c> at 03:00
    /// every night. The results land in <c>/data/exports</c> on the studio's own volume, masked like
    /// every other export; <c>GET /api/schedule</c> reports what each job last did.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithScheduledQueries(
        this IResourceBuilder<WebDataStudioResource> builder, params ScheduledStudioQuery[] jobs)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var named = (jobs ?? [])
            .Where(job => job is not null
                && !string.IsNullOrWhiteSpace(job.Name)
                && !string.IsNullOrWhiteSpace(job.Connection)
                && !string.IsNullOrWhiteSpace(job.Sql))
            .ToList();

        if (named.Count == 0) return builder;

        foreach (var job in named)
        {
            if (job.EveryMinutes is null && string.IsNullOrWhiteSpace(job.DailyAtUtc))
                throw new ArgumentException(
                    $"the scheduled query '{job.Name}' says neither EveryMinutes nor DailyAtUtc, " +
                    "so it would never run", nameof(jobs));

            if (job.EveryMinutes is { } minutes and < 1)
                throw new ArgumentOutOfRangeException(nameof(jobs), minutes,
                    $"'{job.Name}' cannot run more often than once a minute");

            builder.Resource.ScheduleList.RemoveAll(existing =>
                existing.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase));

            builder.Resource.ScheduleList.Add(job);
        }

        // Written on every call, so the last one wins and the file matches the app host.
        var file = Write(builder);

        return builder
            .WithBindMount(file, ScheduleTarget, isReadOnly: true)
            .WithEnvironment("WDS_SCHEDULE_FILE", ScheduleTarget)
            .WithEnvironment("WDS_SCHEDULE_OUTPUT_DIR", OutputTarget);
    }

    /// The generated file, next to the app host's own output so it is not left in a source folder.
    private static string Write(IResourceBuilder<WebDataStudioResource> builder)
    {
        var directory = Path.Combine(Path.GetTempPath(),
            $"webdatastudio-schedule-{builder.Resource.Name}");

        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "schedule.json");

        File.WriteAllText(file, JsonSerializer.Serialize(
            builder.Resource.ScheduleList,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

        return file;
    }
}
