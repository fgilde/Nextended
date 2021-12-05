using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Nextended.Cache.Extensions;
using Nextended.Core.Extensions;
using CacheItemPriority = Microsoft.Extensions.Caching.Memory.CacheItemPriority;

namespace Nextended.Cache
{
    /// <summary>
    /// CacheProvider
    /// </summary>
    public class CacheProvider
    {
        private readonly BlockingCollection<Func<CacheProvider, bool>> clearConditions = new BlockingCollection<Func<CacheProvider, bool>>();
        private readonly IMemoryCache innerCache;
        private Task checkTask;
        private CancellationTokenSource resetCacheToken = new();

        public MemoryCacheEntryOptions CacheEntryOptions { get; set; } = new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.Normal)
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));

        private MemoryCacheEntryOptions defaultCacheEntryOptions => CacheEntryOptions
            .AddExpirationToken(new CancellationChangeToken(resetCacheToken.Token));

        public event EventHandler<EventArgs> Cleared;

        public TimeSpan ClearCheckInterval { get; set; } = TimeSpan.FromMinutes(10);

        public CacheProvider(IMemoryCache cache = null, MemoryCacheEntryOptions cacheEntryOptions = null)
        {
            innerCache = cache ?? new Microsoft.Extensions.Caching.Memory.MemoryCache(Options.Create(new MemoryCacheOptions()));
            CacheEntryOptions = cacheEntryOptions ?? CacheEntryOptions;
        }


        public CacheProvider(IOptions<MemoryCacheOptions> options, MemoryCacheEntryOptions cacheEntryOptions = null)
        {
            innerCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(options);
            CacheEntryOptions = cacheEntryOptions ?? CacheEntryOptions;
        }

        public DateTime LastWriteTime { get; set; }

        public int Count()
        {
            return innerCache.ExposeField<IDictionary>("_entries")?.Count ?? 0;
        }

        public void Clear()
        {
            if (resetCacheToken != null && !resetCacheToken.IsCancellationRequested && resetCacheToken.Token.CanBeCanceled)
            {
                resetCacheToken.Cancel();
                resetCacheToken.Dispose();
            }
            resetCacheToken = new CancellationTokenSource();
            OnCleared();
        }

        public T ExecuteWithCache<TInstance, T>(TInstance owner, Expression<Func<TInstance, T>> expression)
        {
            var info = owner.ExecuteWithCache(innerCache, expression, defaultCacheEntryOptions);
            LastWriteTime = LastWriteTime == default || info.IsNewEntry ? DateTime.Now : LastWriteTime;
            if (!info.IsNewEntry && (checkTask == null || checkTask.IsCanceled || checkTask.IsCompleted))
                checkTask = StartCheckTask(resetCacheToken.Token);
            return info.Result;
        }

        public CacheProvider ClearWhen(Func<CacheProvider, bool> predicate)
        {
            clearConditions.Add(predicate);
            return this;
        }

        private bool ShouldClear()
        {
            return clearConditions.Any(func => func(this));
        }

        private Task StartCheckTask(CancellationToken cancellation = default)
        {
            return Task.Run(() =>
            {
                try
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        if (ShouldClear())
                            Clear();
                        else
                            Task.Delay(ClearCheckInterval, cancellation).Wait(cancellation);
                    }
                }
                catch (TaskCanceledException)
                { }
            }, cancellation);
        }

        protected virtual void OnCleared()
        {
            Cleared?.Invoke(this, EventArgs.Empty);
        }
    }

    public static class CacheProviderExtensions
    {
        public static T ExecuteWithCache<TInstance, T>(this TInstance owner, CacheProvider cache, Expression<Func<TInstance, T>> expression)
        {
            return cache.ExecuteWithCache(owner, expression);
        }
    }
}