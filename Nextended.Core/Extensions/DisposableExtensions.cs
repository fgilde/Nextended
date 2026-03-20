using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Nextended.Core.Extensions;


public static class DisposableExtensions
{
    // ── Adapter: sync ↔ async ────────────────────────────────────────────────

    /// <summary>Wraps an <see cref="IDisposable"/> so it can be used as <see cref="IAsyncDisposable"/>.</summary>
    public static IAsyncDisposable AsAsyncDisposable(this IDisposable d)
        => new SyncAsAsyncDisposable(d);

    /// <summary>Wraps an <see cref="IAsyncDisposable"/> so it can be used as <see cref="IDisposable"/>.
    /// The async DisposeAsync is run synchronously – use with care (no deadlocks on thread-pool!).</summary>
    public static IDisposable AsDisposable(this IAsyncDisposable d)
        => new AsyncAsSyncDisposable(d);

    // ── Combine ──────────────────────────────────────────────────────────────

    /// <summary>Combines multiple <see cref="IDisposable"/> into one.
    /// All are disposed in reverse order; all exceptions are collected and re-thrown as <see cref="AggregateException"/>.</summary>
    public static IDisposable Combine(params IDisposable[] disposables)
        => new CombinedDisposable(disposables);

    /// <inheritdoc cref="Combine(IDisposable[])"/>
    public static IDisposable Combine(IEnumerable<IDisposable> disposables)
        => new CombinedDisposable(disposables);

    /// <summary>Combines multiple <see cref="IAsyncDisposable"/> into one async disposable.
    /// All are disposed in reverse order; all exceptions are collected and re-thrown as <see cref="AggregateException"/>.</summary>
    public static IAsyncDisposable CombineAsync(params IAsyncDisposable[] disposables)
        => new CombinedAsyncDisposable(disposables);

    /// <inheritdoc cref="CombineAsync(IAsyncDisposable[])"/>
    public static IAsyncDisposable CombineAsync(IEnumerable<IAsyncDisposable> disposables)
        => new CombinedAsyncDisposable(disposables);

    /// <summary>Combines this <see cref="IDisposable"/> with one or more others into a single disposable.</summary>
    public static IDisposable CombineWith(this IDisposable first, params IDisposable[] others)
        => new CombinedDisposable([first, .. others]);

    /// <summary>Combines this <see cref="IAsyncDisposable"/> with one or more others into a single async disposable.</summary>
    public static IAsyncDisposable CombineWith(this IAsyncDisposable first, params IAsyncDisposable[] others)
        => new CombinedAsyncDisposable([first, .. others]);

    // ── Action-based ─────────────────────────────────────────────────────────

    /// <summary>Creates an <see cref="IDisposable"/> that runs <paramref name="onDispose"/> exactly once.</summary>
    public static IDisposable ToDisposable(this Action onDispose)
        => new ActionDisposable(onDispose);

    /// <summary>Creates an <see cref="IAsyncDisposable"/> that runs <paramref name="onDispose"/> exactly once.</summary>
    public static IAsyncDisposable ToAsyncDisposable(this Func<ValueTask> onDispose)
        => new AsyncActionDisposable(onDispose);

    // ── Null-safety ──────────────────────────────────────────────────────────

    /// <summary>Disposes <paramref name="d"/> if it is not null.</summary>
    public static void DisposeIfNotNull(this IDisposable? d) => d?.Dispose();

    /// <summary>Disposes <paramref name="d"/> if it is not null.</summary>
    public static ValueTask DisposeIfNotNullAsync(this IAsyncDisposable? d)
        => d?.DisposeAsync() ?? default;

    // ── Swap / replace helper ────────────────────────────────────────────────

    /// <summary>Disposes <paramref name="current"/>, assigns <paramref name="next"/> to it and returns <paramref name="next"/>.</summary>
    public static T Swap<T>(ref T? current, T next) where T : IDisposable
    {
        current?.Dispose();
        current = next;
        return next;
    }

    // ── Deferred / lazy ──────────────────────────────────────────────────────

    /// <summary>Wraps a disposable in a lazy container.
    /// The inner disposable is only created when first accessed and disposed exactly once.</summary>
    public static IDisposable Defer<T>(Func<T> factory) where T : IDisposable
        => new DeferredDisposable<T>(factory);

    // ════════════════════════════════════════════════════════════════════════
    // Private implementations
    // ════════════════════════════════════════════════════════════════════════

    private sealed class SyncAsAsyncDisposable(IDisposable inner) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            inner.Dispose();
#if !NETSTANDARD
            return ValueTask.CompletedTask;
#else
            return default;
#endif
        }
    }

    private sealed class AsyncAsSyncDisposable(IAsyncDisposable inner) : IDisposable
    {
        public void Dispose() => inner.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class CombinedDisposable : IDisposable
    {
        private readonly IDisposable[] _items;
        private int _disposed;

        public CombinedDisposable(IEnumerable<IDisposable> items)
            => _items = items.ToArray();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            List<Exception>? errors = null;
            foreach (var item in Enumerable.Reverse(_items))
            {
                try { item.Dispose(); }
                catch (Exception ex) { (errors ??= []).Add(ex); }
            }
            if (errors is { Count: > 0 })
                throw new AggregateException("One or more disposables threw during Dispose.", errors);
        }
    }

    private sealed class CombinedAsyncDisposable : IAsyncDisposable
    {
        private readonly IAsyncDisposable[] _items;
        private int _disposed;

        public CombinedAsyncDisposable(IEnumerable<IAsyncDisposable> items)
            => _items = items.ToArray();

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            List<Exception>? errors = null;
            foreach (var item in Enumerable.Reverse(_items))
            {
                try { await item.DisposeAsync().ConfigureAwait(false); }
                catch (Exception ex) { (errors ??= []).Add(ex); }
            }
            if (errors is { Count: > 0 })
                throw new AggregateException("One or more disposables threw during DisposeAsync.", errors);
        }
    }

    private sealed class ActionDisposable(Action action) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                action();
        }
    }

    private sealed class AsyncActionDisposable(Func<ValueTask> action) : IAsyncDisposable
    {
        private int _disposed;
        public ValueTask DisposeAsync()
            => Interlocked.Exchange(ref _disposed, 1) == 0
                ? action()
                : default;
    }

    private sealed class DeferredDisposable<T>(Func<T> factory) : IDisposable where T : IDisposable
    {
        private readonly Lazy<T> _lazy = new(factory, LazyThreadSafetyMode.ExecutionAndPublication);
        private int _disposed;

        public T Value => _lazy.Value;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            if (_lazy.IsValueCreated)
                _lazy.Value.Dispose();
        }
    }
}