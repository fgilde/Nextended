using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Nextended.Core.Extensions;

namespace Nextended.Cache.Extensions
{
    /// <summary>
    /// Simple cache extensions
    /// </summary>
    public static class CacheExtensions
    {
        public class CacheExecutionInfo<T>
        {
            public CacheExecutionInfo(T result, string key, bool isNewEntry)
            {
                Result = result;
                Key = key;
                IsNewEntry = isNewEntry;
            }

            public T Result { get; }
            public string Key { get; }
            public bool IsNewEntry { get; }
        }

        /// <summary>
        /// Adds a cache entry into the cache using the specified key and a Valuefactory and an absolute expiration value
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="key">A unique identifier for the cache entry to add.</param>
        /// <param name="valueFactory">The value factory.</param>
        /// <param name="absoluteExpiration">The fixed date and time at which the cache entry will expire.</param>
        /// <param name="regionName">A named region in the cache to which a cache entry can be added. Do not pass a value for this parameter. This parameter is null by default, because the MemoryCache class does not implement regions.</param>
        /// <returns>If a cache entry with the same key exists, the existing cache entry; otherwise, null.</returns>
        public static T AddOrGetExisting<T>(this ObjectCache cache, string key, Func<T> valueFactory,
            DateTimeOffset absoluteExpiration = default, string regionName = null)
        {
            if (absoluteExpiration == default)
                absoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration;

            Lazy<T> newValue = new Lazy<T>(valueFactory);
            Lazy<T> value = (Lazy<T>)cache.AddOrGetExisting(key, newValue, absoluteExpiration, regionName);
            return (value ?? newValue).Value;
        }

        public static CacheExecutionInfo<TResult> ExecuteWithCache<TParam, TResult>(this TParam param, IMemoryCache cache, string cacheKey,
            Func<TParam, TResult> func, MemoryCacheEntryOptions entryOptions = null)
        {
            bool isNewEntry = false;
            return new CacheExecutionInfo<TResult>(cache.GetOrCreate(cacheKey, entry =>
            {
                isNewEntry = true;
                if (entryOptions != null)
                    entry.SetOptions(entryOptions);
                return func(param);
            }), cacheKey, isNewEntry);
        }

        public static CacheExecutionInfo<TResult> ExecuteWithCache<TParam, TResult>(this TParam param, IMemoryCache cache, Func<TParam, TResult> func, MemoryCacheEntryOptions entryOptions = null)
        {
            var cacheKey = GetCacheKey(func);
            return ExecuteWithCache(param, cache, cacheKey, func, entryOptions);
        }

        public static CacheExecutionInfo<TResult> ExecuteWithCache<TParam, TResult>(this TParam param, IMemoryCache cache, Expression<Func<TParam, TResult>> expression, MemoryCacheEntryOptions entryOptions = null)
        {
            var cacheKey = GetCacheKey(expression);
            return ExecuteWithCache(param, cache, cacheKey, expression.Compile(), entryOptions);
        }
        
        public static MemoryCacheEntryOptions MemoryCacheEntryOptions(this IMemoryCache cache, CancellationToken cancellationToken = default)
        {
            return new MemoryCacheEntryOptions()
                .SetPriority(Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal)
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .AddExpirationToken(new CancellationChangeToken(cancellationToken));
        }

        private static string GetCacheKey<TParam, TResult>(Expression<Func<TParam, TResult>> expression)
        {
            var methodCallExpression = (MethodCallExpression)expression.Body;
            var objectName = methodCallExpression.Object?.ToString().Split('.').Last();
            var parameters = methodCallExpression.ReadParameters();
            var paramStr = string.Join(",", parameters.Select(x => x.Key + "=" + x.Value).ToArray());

            return $"{typeof(TParam).FullName}_{typeof(TResult).FullName}={objectName}.{methodCallExpression.Method.Name}({paramStr})";
        }

        private static string GetCacheKey<TParam, TResult>(Func<TParam, TResult> func)
        {
            return GetCacheKey(func.ToExpression());
        }

    }
}