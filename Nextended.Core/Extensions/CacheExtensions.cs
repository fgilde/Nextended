using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;

namespace Nextended.Core.Extensions
{
    /// <summary>
    /// Simple cache extensions
    /// </summary>
    public static class CacheExtensions
    {
        //public class CacheExecutionInfo<T>
        //{
        //    public CacheExecutionInfo(T result, string key, bool isNewEntry)
        //    {
        //        Result = result;
        //        Key = key;
        //        IsNewEntry = isNewEntry;
        //    }

        //    public T Result { get; }
        //    public string Key { get; }
        //    public bool IsNewEntry { get; }
        //}

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


        //public static CacheExecutionInfo<T> ExecuteWithCache<TInstance, T>(this TInstance client, IMemoryCache cache, Expression<Func<TInstance, T>> expression, MemoryCacheEntryOptions entryOptions)
        //{
        //    var cacheKey = GetCacheKey(expression);
        //    bool isNewEntry = false;
        //    return new CacheExecutionInfo<T>(cache.GetOrCreate(cacheKey, entry =>
        //    {
        //        isNewEntry = true;
        //        if (entryOptions != null)
        //            entry.SetOptions(entryOptions);
        //        return expression.Compile()(client);
        //    }), cacheKey, isNewEntry);
        //}

        //public static object MemoryCacheEntryOptions(this IMemoryCache cache, CancellationToken cancellationToken = default)
        //{
        //    return new MemoryCacheEntryOptions()
        //        .SetPriority(CacheItemPriority.Normal)
        //        .SetAbsoluteExpiration(TimeSpan.FromHours(1))
        //        .AddExpirationToken(new CancellationChangeToken(cancellationToken));
        //}

        private static string GetCacheKey<TInstance, T>(Expression<Func<TInstance, T>> expression)
        {
            var methodCallExpression = (MethodCallExpression)expression.Body;
            var objectName = methodCallExpression.Object?.ToString().Split('.').Last();
            var parameters = methodCallExpression.ReadParameters();
            var paramStr = string.Join(",", parameters.Select(x => x.Key + "=" + x.Value).ToArray());

            return $"{typeof(TInstance).FullName}_{typeof(T).FullName}={objectName}.{methodCallExpression.Method.Name}({paramStr})";
        }


    }
}