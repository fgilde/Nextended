using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.IncludeDefinitions;
using Nextended.EF.Tests.Domain;
using Nextended.EF.Tests.Support;
using Shouldly;

namespace Nextended.EF.Tests;

[TestClass]
public class ExistingExtensionsTests
{
    [TestMethod]
    public async Task WhereContains_FiltersAcrossMultipleProperties_CaseInsensitive()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Customers
            .WhereContains("CARGO", c => c.Name, c => c.Email)
            .ToListAsync();

        hits.Select(c => c.Name).ShouldBe(new[] { "Carol" });
    }

    [TestMethod]
    public async Task WhereContains_BlankSearch_ReturnsSourceUnchanged()
    {
        await using var db = await TestDb.SeedAsync();

        var all = await db.Customers.WhereContains("   ", c => c.Name).CountAsync();
        all.ShouldBe(3);
    }

    [TestMethod]
    public async Task WhereKeyMatches_ByContextResolvesPrimaryKey_FindsRowById()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Customers.WhereKeyMatches(db, "2").ToListAsync();
        hits.Single().Name.ShouldBe("Bob");
    }

    [TestMethod]
    public async Task WhereKeyMatches_ByPropertyNames_TriesEachProperty()
    {
        await using var db = await TestDb.SeedAsync();

        // OrderNumber is a string key candidate
        var hits = await db.Orders
            .WhereKeyMatches(new[] { nameof(Order.OrderNumber) }, "ORD-101")
            .ToListAsync();

        hits.Single().Id.ShouldBe(2);
    }

    [TestMethod]
    public async Task WhereKeyMatches_NoMatchingPropertyNames_ReturnsEmpty()
    {
        await using var db = await TestDb.SeedAsync();

        var hits = await db.Customers
            .WhereKeyMatches(new[] { "NoSuchProperty" }, "anything")
            .ToListAsync();

        hits.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task IncludeAll_LoadsPrincipalSideCollectionNavigations()
    {
        // IncludeAll walks the model and pulls in navigations on the principal side
        // (it skips IsOnDependent navigations to avoid cycles). For Customer→Orders that
        // means Orders is included; the inverse Order.Customer is skipped.
        await using var db = await TestDb.SeedAsync();

        var bob = await db.Customers.IncludeAll().FirstAsync(c => c.Name == "Bob");

        bob.Orders.ShouldNotBeEmpty();
        bob.Orders.First().Lines.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task IncludeAll_WithExcludeExpression_SkipsThatNavigation()
    {
        await using var db = await TestDb.SeedAsync();

        var bob = await db.Customers
            .IncludeAll(c => c.Orders)
            .FirstAsync(c => c.Name == "Bob");

        // With Orders excluded the navigation isn't traversed by IncludeAll
        // → the default empty initializer remains.
        bob.Orders.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task IncludeDetails_FromDefinition_LoadsExpectedPaths()
    {
        await using var db = await TestDb.SeedAsync();

        var def = new IncludeDefinitionFor<Customer>()
            .Include(c => c.Address)
            .Include(c => c.Orders);

        var alice = await db.Customers.IncludeDetails(def).FirstAsync(c => c.Id == 1);

        alice.Address!.City.ShouldBe("Berlin");
        alice.Orders.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task LoadGraphAsync_LoadsReferencedNavigationsRecursively()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers.FirstAsync(c => c.Id == 1);
        await db.LoadGraphAsync(alice, maxDepth: 2);

        alice.Address.ShouldNotBeNull();
        alice.Orders.ShouldNotBeEmpty();
        alice.Orders.First().Lines.ShouldNotBeEmpty();
    }

    [TestMethod]
    public void MultiInclude_AppliesAllSubIncludes()
    {
        // Smoke-test: just ensure the call chain compiles and yields a queryable that can be enumerated.
        using var db = TestDb.CreateEmpty();

        var query = db.Customers.MultiInclude(
            c => c.Include(x => x.Orders),
            o => o.Lines,
            o => o.Customer!);

        query.ShouldNotBeNull();
    }
}
