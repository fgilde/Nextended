using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.EF.Tests.Domain;
using Nextended.EF.Tests.Support;
using Shouldly;

namespace Nextended.EF.Tests;

[TestClass]
public class DbContextHelperTests
{
    [TestMethod]
    public async Task FindEntityType_ReturnsRegisteredEntity()
    {
        await using var db = TestDb.CreateEmpty();
        var entityType = db.FindEntityType<Customer>();

        entityType.ShouldNotBeNull();
        entityType!.ClrType.ShouldBe(typeof(Customer));
    }

    [TestMethod]
    public async Task GetPrimaryKeyPropertyNames_ReturnsPkProperties()
    {
        await using var db = TestDb.CreateEmpty();
        db.GetPrimaryKeyPropertyNames<Customer>().ShouldBe(new[] { nameof(Customer.Id) });
    }

    [TestMethod]
    public async Task GetPrimaryKeyValues_OfTrackedEntity_ReturnsKeyValues()
    {
        await using var db = await TestDb.SeedAsync();

        var bob = await db.Customers.FirstAsync(c => c.Id == 2);
        var keys = db.GetPrimaryKeyValues(bob);

        keys.ShouldNotBeNull();
        keys!.Length.ShouldBe(1);
        keys[0].ShouldBe(2);
    }

    [TestMethod]
    public async Task DetachAll_ClearsTracker()
    {
        await using var db = await TestDb.SeedAsync();

        // Touch some entities so they're tracked
        await db.Customers.AsTracking().FirstAsync(c => c.Id == 1);
        db.ChangeTracker.Entries().Any().ShouldBeTrue();

        db.DetachAll();

        db.ChangeTracker.Entries().Any().ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetOrAddAsync_ExistingMatch_ReturnsExistingWithoutInsert()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers.GetOrAddAsync(
            c => c.Name == "Alice",
            () => new Customer { Name = "Alice" });

        alice.Id.ShouldBe(1);
        db.ChangeTracker.Entries<Customer>().Any(e => e.State == EntityState.Added).ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetOrAddAsync_NoMatch_AddsEntityWithoutSaveChanges()
    {
        await using var db = await TestDb.SeedAsync();

        var dora = await db.Customers.GetOrAddAsync(
            c => c.Name == "Dora",
            () => new Customer { Name = "Dora", CreditLimit = 200 });

        dora.Name.ShouldBe("Dora");
        db.ChangeTracker.Entries<Customer>().Any(e => e.State == EntityState.Added).ShouldBeTrue();

        // Not saved yet
        (await db.Customers.AsNoTracking().AnyAsync(c => c.Name == "Dora")).ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetOrCreateAsync_NoMatch_AddsAndPersists()
    {
        await using var db = await TestDb.SeedAsync();

        var dora = await db.GetOrCreateAsync(
            db.Customers,
            c => c.Name == "Dora",
            () => new Customer { Name = "Dora", CreditLimit = 200 });

        dora.Name.ShouldBe("Dora");
        (await db.Customers.AsNoTracking().AnyAsync(c => c.Name == "Dora")).ShouldBeTrue();
    }

    [TestMethod]
    public async Task IsTrackedBy_ReflectsTrackerState()
    {
        await using var db = await TestDb.SeedAsync();

        var alice = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == 1);
        db.IsTrackedBy(alice).ShouldBeFalse();

        db.Attach(alice);
        db.IsTrackedBy(alice).ShouldBeTrue();
    }
}
