using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nextended.Core.Extensions;

namespace Nextended.Web;


//[RegisterAs(typeof(BackgroundExecutor), RegisterAsImplementation = true)]

public sealed class BackgroundExecutor(
    IServiceScopeFactory scopeFactory,
    ILogger log,
    IHttpContextAccessor httpContextAccessor) 
{
    private Task ExecuteDetachedCoreAsync(
        HttpRequestSnapshot? snapshot,
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        IReadOnlyList<IAsyncDisposable>? disposables,
        Func<IServiceProvider, CancellationToken, Task>? onSetup,
        Func<IServiceProvider, Exception?, CancellationToken, Task>? onTeardown)
    {
        _ = Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(timeout);
            Exception? actionException = null;
            try
            {
                await using var exec = new BackgroundExecutionScope(scopeFactory, snapshot);

                if (onSetup is not null)
                    await onSetup(exec.Services, cts.Token);

                try
                {
                    await action(exec.Services, cts.Token);
                    await exec.CompleteAsync(cts.Token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    actionException = ex;
                    throw;
                }
                finally
                {
                    if (onTeardown is not null)
                        try { await onTeardown(exec.Services, actionException, cts.Token); }
                        catch (Exception ex) { log.LogWarning(ex, "Teardown failed."); }
                }
            }
            catch (OperationCanceledException)
            {
                log.LogWarning("Detached background action timed out.");
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Detached background action failed.");
            }
            finally
            {
                if (disposables is not null)
                    foreach (var d in disposables.Reverse())
                        try { await d.DisposeAsync(); }
                        catch (Exception ex) { log.LogWarning(ex, "Disposable cleanup failed: {Type}", d.GetType().Name); }
            }
        });
        return Task.CompletedTask;
    }

    // ── Öffentliche Overloads ────────────────────────────────────────────────────

    public Task ExecuteDetachedAsync(
        HttpRequestSnapshot? snapshot,
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action)
        => ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables: null, onSetup: null, onTeardown: null);

    public Task ExecuteDetachedAsync(
        HttpRequestSnapshot? snapshot,
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        IReadOnlyList<IAsyncDisposable> disposables)
        => ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables, onSetup: null, onTeardown: null);

    public Task ExecuteDetachedAsync(
        HttpRequestSnapshot? snapshot,
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        IReadOnlyList<IDisposable> disposables)
        => ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables.Select(d => d.AsAsyncDisposable()).ToList(),
            onSetup: null, onTeardown: null);

    public Task ExecuteDetachedAsync(
        HttpRequestSnapshot? snapshot,
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        Func<IServiceProvider, CancellationToken, Task>? onSetup = null,
        Func<IServiceProvider, Exception?, CancellationToken, Task>? onTeardown = null)
        => ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables: null, onSetup, onTeardown);

    // ── Mit T-Rückgabe ───────────────────────────────────────────────────────────

    public Task<T?> ExecuteDetachedAsync<T>(
        HttpRequestSnapshot? snapshot,
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task<T>> action)
    {
        T? result = default;
        return ExecuteDetachedCoreAsync(
            snapshot, timeout,
            async (sp, ct) => result = await action(sp, ct),
            disposables: null, onSetup: null, onTeardown: null)
            .ContinueWith(_ => result, TaskScheduler.Default);
    }

    // ── CaptureSnapshot-Varianten ────────────────────────────────────────────────

    public async Task ExecuteDetachedWithCapturedRequestAsync(
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        CancellationToken captureCt = default)
    {
        var snapshot = await CaptureSnapshotAsync(captureCt);
        await ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables: null, onSetup: null, onTeardown: null);
    }

    public async Task ExecuteDetachedWithCapturedRequestAsync(
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        IReadOnlyList<IAsyncDisposable> disposables,
        CancellationToken captureCt = default)
    {
        var snapshot = await CaptureSnapshotAsync(captureCt);
        await ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables, onSetup: null, onTeardown: null);
    }

    public async Task ExecuteDetachedWithCapturedRequestAsync(
        TimeSpan timeout,
        Func<IServiceProvider, CancellationToken, Task> action,
        IReadOnlyList<IDisposable> disposables,
        CancellationToken captureCt = default)
    {
        var snapshot = await CaptureSnapshotAsync(captureCt);
        await ExecuteDetachedCoreAsync(snapshot, timeout, action,
            disposables.Select(d => d.AsAsyncDisposable()).ToList(),
            onSetup: null, onTeardown: null);
    }
    private async Task<HttpRequestSnapshot?> CaptureSnapshotAsync(CancellationToken ct)
    {
        var ctx = httpContextAccessor.HttpContext;
        return ctx is null ? null : await ctx.CaptureAsync(ct);
    }
}