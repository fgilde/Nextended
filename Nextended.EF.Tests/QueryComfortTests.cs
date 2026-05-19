using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.EF.Tests.Domain;
using Nextended.EF.Tests.Support;
using Shouldly;

namespace Nextended.EF.Tests;

[TestClass]
public class QueryComfortTests
{
    [TestMethod]
    public async Task WhereBetween_IncludesBothBounds()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Orders
            .WhereBetween(o => o.TotalCost, 10m, 30m)
            .OrderBy(o => o.Id)
            .ToListAsync();

        hits.Select(o => o.OrderNumber).ShouldBe(new[] { "ORD-101" });
    }

    [TestMethod]
    public async Task WhereBetween_OnDateTime_FiltersRange()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Orders
            .WhereBetween(o => o.CreatedAt,
                new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Utc))
            .OrderBy(o => o.Id)
            .ToListAsync();

        hits.Select(o => o.Id).ShouldBe(new[] { 2, 3 });
    }

    [TestMethod]
    public async Task WhereIn_FiltersByCollection()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Customers.WhereIn(c => c.Id, new[] { 1, 3 }).OrderBy(c => c.Id).ToListAsync();
        hits.Select(c => c.Name).ShouldBe(new[] { "Alice", "Carol" });
    }

    [TestMethod]
    public async Task WhereIn_EmptyCollection_YieldsEmptyResult()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Customers.WhereIn(c => c.Id, System.Array.Empty<int>()).ToListAsync();
        hits.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task IncludeIf_True_LoadsNavigation()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers
            .AsNoTracking()
            .IncludeIf(true, c => c.Address)
            .FirstAsync(c => c.Id == 1);

        alice.Address.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task IncludeIf_False_NavigationStaysNull()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers
            .AsNoTracking()
            .IncludeIf(false, c => c.Address)
            .FirstAsync(c => c.Id == 1);

        alice.Address.ShouldBeNull();
    }

    [TestMethod]
    public async Task IncludeIf_StringOverload_LoadsByPath()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers.AsNoTracking().IncludeIf(true, "Address").FirstAsync(c => c.Id == 1);
        alice.Address.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task AsNoTrackingIf_True_DisablesTracking()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers.AsNoTrackingIf(true).FirstAsync(c => c.Id == 1);
        db.IsTrackedBy(alice).ShouldBeFalse();
    }

    [TestMethod]
    public async Task AsTrackingIf_True_EnablesTracking()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers.AsNoTracking().AsTrackingIf(true).FirstAsync(c => c.Id == 1);
        db.IsTrackedBy(alice).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExistsAsync_WithPredicate_ReflectsData()
    {
        await using var db = await TestDb.SeedAsync();

        (await db.Customers.ExistsAsync(c => c.Name == "Bob")).ShouldBeTrue();
        (await db.Customers.ExistsAsync(c => c.Name == "Zoe")).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ExistsAsync_WithoutPredicate_ChecksAny()
    {
        await using var db = await TestDb.SeedAsync();

        (await db.Customers.ExistsAsync()).ShouldBeTrue();

        await using var empty = TestDb.CreateEmpty();
        (await empty.Customers.ExistsAsync()).ShouldBeFalse();
    }
}
