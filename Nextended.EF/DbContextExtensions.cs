using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Nextended.EF;

/// <summary>
/// Helpers around <see cref="DbContext"/> for metadata, change-tracker manipulation, and the
/// find-or-create pattern.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>Returns the <see cref="IEntityType"/> registered for <typeparamref name="T"/>, or null.</summary>
    public static IEntityType? FindEntityType<T>(this DbContext context) where T : class
        => context.Model.FindEntityType(typeof(T));

    /// <summary>
    /// Returns the primary-key property names for <typeparamref name="T"/> in declaration order.
    /// Empty if no PK is configured.
    /// </summary>
    public static IReadOnlyList<string> GetPrimaryKeyPropertyNames<T>(this DbContext context) where T : class
    {
        var et = context.FindEntityType<T>();
        var pk = et?.FindPrimaryKey();
        if (pk is null) return Array.Empty<string>();
        return pk.Properties.Select(p => p.Name).ToArray();
    }

    /// <summary>
    /// Returns the primary-key values for <paramref name="entity"/> in declaration order
    /// (handles composite keys). Returns null if no PK is configured.
    /// </summary>
    public static object?[]? GetPrimaryKeyValues<T>(this DbContext context, T entity) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        var et = context.FindEntityType<T>();
        var pk = et?.FindPrimaryKey();
        if (pk is null) return null;

        var entry = context.Entry(entity);
        return pk.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray();
    }

    /// <summary>Detaches every tracked entity from the change tracker.</summary>
    public static void DetachAll(this DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries().ToArray())
            entry.State = EntityState.Detached;
    }

    /// <summary>
    /// Look for an entity matching <paramref name="predicate"/>; if none, build one with
    /// <paramref name="factory"/> and add it to the set. Does NOT call SaveChanges.
    /// </summary>
    public static async Task<T> GetOrAddAsync<T>(
        this DbSet<T> set,
        Expression<Func<T, bool>> predicate,
        Func<T> factory,
        CancellationToken cancellationToken = default) where T : class
    {
        var existing = await set.FirstOrDefaultAsync(predicate, cancellationToken);
        if (existing is not null) return existing;
        var created = factory();
        await set.AddAsync(created, cancellationToken);
        return created;
    }

    /// <summary>
    /// Look for an entity matching <paramref name="predicate"/>; if none, build one with
    /// <paramref name="factory"/>, add it and persist (SaveChangesAsync).
    /// </summary>
    public static async Task<T> GetOrCreateAsync<T>(
        this DbContext context,
        DbSet<T> set,
        Expression<Func<T, bool>> predicate,
        Func<T> factory,
        CancellationToken cancellationToken = default) where T : class
    {
        var existing = await set.FirstOrDefaultAsync(predicate, cancellationToken);
        if (existing is not null) return existing;
        var created = factory();
        await set.AddAsync(created, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return created;
    }

    /// <summary>
    /// True if <paramref name="entity"/> is currently being tracked by <paramref name="context"/>
    /// (in any state other than Detached).
    /// </summary>
    public static bool IsTrackedBy<T>(this DbContext context, T entity) where T : class
    {
        if (entity is null) return false;
        var entry = context.ChangeTracker.Entries<T>().FirstOrDefault(e => ReferenceEquals(e.Entity, entity));
        return entry is not null && entry.State != EntityState.Detached;
    }
}
