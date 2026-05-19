using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Nextended.EF;

/// <summary>
/// Bulk-style operations layered on top of EF Core 7+'s <c>ExecuteUpdateAsync</c>/<c>ExecuteDeleteAsync</c>
/// plus convenience batchers for insert and upsert that don't require a third-party library.
/// </summary>
/// <remarks>
/// <para>
/// <c>BulkUpdateWhereAsync</c>/<c>BulkDeleteWhereAsync</c> issue a single server-side statement and
/// bypass the change tracker, mirroring the spirit of commercial bulk libraries with what EF Core
/// itself ships. On the InMemory provider EF Core emulates them.
/// </para>
/// <para>
/// <c>BulkInsertAsync</c> and <c>UpsertRangeAsync</c> are tracker-based: they batch <c>AddRange</c>
/// (and lookups by key for upsert) and call <c>SaveChangesAsync</c> once. Far cheaper than per-entity
/// SaveChanges, though not as fast as native bulk APIs.
/// </para>
/// </remarks>
public static class BulkExtensions
{
    /// <summary>
    /// Delete every row matching <paramref name="predicate"/>.
    /// On relational providers issues a single server-side statement via
    /// <see cref="EntityFrameworkQueryableExtensions.ExecuteDeleteAsync"/>.
    /// On providers that don't support bulk SQL (e.g. InMemory) falls back to a tracker-based
    /// load + RemoveRange + SaveChanges round-trip so the call still works in tests.
    /// </summary>
    public static async Task<int> BulkDeleteWhereAsync<T>(
        this DbSet<T> set,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            return await set.Where(predicate).ExecuteDeleteAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            var context = GetContext(set);
            var matches = await set.Where(predicate).ToListAsync(cancellationToken);
            if (matches.Count == 0) return 0;
            set.RemoveRange(matches);
            await context.SaveChangesAsync(cancellationToken);
            return matches.Count;
        }
    }

    private static DbContext GetContext<T>(DbSet<T> set) where T : class
    {
        var provider = ((IInfrastructure<IServiceProvider>)set).Instance;
        if (provider.GetService(typeof(ICurrentDbContext)) is ICurrentDbContext current)
            return current.Context;
        throw new InvalidOperationException("Could not resolve DbContext from DbSet.");
    }

    /// <summary>
    /// Add <paramref name="entities"/> to the set and SaveChanges once. Optionally splits very large
    /// payloads into <paramref name="batchSize"/>-sized chunks so each SaveChanges stays bounded.
    /// </summary>
    /// <returns>Total number of state entries written across all batches.</returns>
    public static async Task<int> BulkInsertAsync<T>(
        this DbContext context,
        IEnumerable<T> entities,
        int batchSize = 1000,
        CancellationToken cancellationToken = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));
        if (batchSize <= 0) batchSize = 1000;

        var set = context.Set<T>();
        var written = 0;
        var buffer = new List<T>(batchSize);

        foreach (var entity in entities)
        {
            buffer.Add(entity);
            if (buffer.Count >= batchSize)
            {
                await set.AddRangeAsync(buffer, cancellationToken);
                written += await context.SaveChangesAsync(cancellationToken);
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            await set.AddRangeAsync(buffer, cancellationToken);
            written += await context.SaveChangesAsync(cancellationToken);
        }

        return written;
    }

    /// <summary>
    /// For each entity, look up an existing row by <paramref name="keySelector"/> and apply
    /// <paramref name="updateExisting"/>; otherwise insert. Saves once at the end.
    /// </summary>
    /// <returns>Total state entries written by the final SaveChanges.</returns>
    public static async Task<int> UpsertRangeAsync<T, TKey>(
        this DbContext context,
        IEnumerable<T> entities,
        Expression<Func<T, TKey>> keySelector,
        Action<T, T>? updateExisting = null,
        CancellationToken cancellationToken = default) where T : class where TKey : notnull
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

        var set = context.Set<T>();
        var keyFn = keySelector.Compile();

        var incoming = entities as IReadOnlyList<T> ?? entities.ToArray();
        if (incoming.Count == 0) return 0;

        var keys = incoming.Select(keyFn).ToArray();
        var existing = await set.WhereIn(keySelector, keys).ToListAsync(cancellationToken);
        var existingByKey = existing.ToDictionary(keyFn);

        foreach (var entity in incoming)
        {
            if (existingByKey.TryGetValue(keyFn(entity), out var match))
            {
                updateExisting?.Invoke(match, entity);
            }
            else
            {
                await set.AddAsync(entity, cancellationToken);
            }
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Single-entity upsert. Returns the existing or newly-added entity.
    /// </summary>
    public static async Task<T> UpsertAsync<T, TKey>(
        this DbContext context,
        T entity,
        Expression<Func<T, TKey>> keySelector,
        Action<T, T>? updateExisting = null,
        CancellationToken cancellationToken = default) where T : class where TKey : notnull
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

        var set = context.Set<T>();
        var keyFn = keySelector.Compile();
        var key = keyFn(entity);

        var match = await set.WhereIn(keySelector, new[] { key }).FirstOrDefaultAsync(cancellationToken);
        if (match is not null)
        {
            updateExisting?.Invoke(match, entity);
            await context.SaveChangesAsync(cancellationToken);
            return match;
        }

        await set.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
