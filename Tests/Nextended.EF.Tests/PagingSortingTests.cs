using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.EF.Tests.Domain;
using Nextended.EF.Tests.Support;
using Shouldly;

namespace Nextended.EF.Tests;

[TestClass]
public class PagingSortingTests
{
    [TestMethod]
    public async Task WhereIf_True_AppliesPredicate()
    {
        await using var db = await TestDb.SeedAsync();

        var rich = await db.Customers
            .WhereIf(true, c => c.CreditLimit > 1000)
            .ToPagedResultAsync(0, 50);

        rich.Items.Count.ShouldBe(1);
        rich.Items[0].Name.ShouldBe("Bob");
    }

    [TestMethod]
    public async Task WhereIf_False_LeavesQueryUntouched()
    {
        await using var db = await TestDb.SeedAsync();

        var all = await db.Customers.WhereIf(false, c => c.CreditLimit > 1_000_000).ToPagedResultAsync(0, 50);
        all.TotalCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task Page_SkipTakeCombo()
    {
        await using var db = await TestDb.SeedAsync();

        var page = db.Customers.OrderBy(c => c.Id).Page(1, 1).ToList();
        page.Single().Id.ShouldBe(2);
    }

    [TestMethod]
    public async Task ToPagedResultAsync_ReturnsItemsAndTotal()
    {
        await using var db = await TestDb.SeedAsync();

        var result = await db.Orders.OrderBy(o => o.Id).ToPagedResultAsync(pageIndex: 0, pageSize: 2);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(2);
        result.PageIndex.ShouldBe(0);
        result.PageSize.ShouldBe(2);
        result.TotalPages.ShouldBe(2);
        result.HasNext.ShouldBeTrue();
        result.HasPrevious.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ToPagedResultAsync_LastPageHasNoNext()
    {
        await using var db = await TestDb.SeedAsync();

        var result = await db.Orders.OrderBy(o => o.Id).ToPagedResultAsync(pageIndex: 1, pageSize: 2);

        result.Items.Count.ShouldBe(1);
        result.HasNext.ShouldBeFalse();
        result.HasPrevious.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ToPagedResultAsync_NegativePageIndex_NormalizedToZero()
    {
        await using var db = await TestDb.SeedAsync();

        var result = await db.Customers.OrderBy(c => c.Id).ToPagedResultAsync(-5, 10);
        result.PageIndex.ShouldBe(0);
        result.Items.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task OrderByMember_SimpleProperty()
    {
        await using var db = await TestDb.SeedAsync();

        var sorted = db.Customers.OrderByMember(nameof(Customer.Name), descending: true).ToList();
        sorted.Select(c => c.Name).ShouldBe(new[] { "Carol", "Bob", "Alice" });
    }

    [TestMethod]
    public async Task OrderByMember_IgnoresCase()
    {
        await using var db = await TestDb.SeedAsync();

        var sorted = db.Customers.OrderByMember("name").ToList();
        sorted.Select(c => c.Name).ShouldBe(new[] { "Alice", "Bob", "Carol" });
    }

    [TestMethod]
    public async Task ThenByMember_ChainsOrdering()
    {
        await using var db = await TestDb.SeedAsync();

        // Two orders for Bob (CustomerId=2), one for Alice. Sort by CustomerId then TotalCost desc.
        var sorted = db.Orders
            .OrderByMember(nameof(Order.CustomerId))
            .ThenByMember(nameof(Order.TotalCost), descending: true)
            .ToList();

        sorted.Select(o => o.Id).ShouldBe(new[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task OrderByMembers_MultiColumnFromTuples()
    {
        await using var db = await TestDb.SeedAsync();

        var sorted = db.Orders.OrderByMembers(new[]
        {
            (nameof(Order.CustomerId), false),
            (nameof(Order.TotalCost), true),
        }).ToList();

        sorted.Select(o => o.Id).ShouldBe(new[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task OrderByMembers_EmptyInput_LeavesSourceUnchanged()
    {
        await using var db = await TestDb.SeedAsync();

        var sorted = db.Orders.OrderByMembers(System.Array.Empty<(string, bool)>()).ToList();
        sorted.Count.ShouldBe(3);
    }

    [TestMethod]
    public void OrderByMember_UnknownProperty_Throws()
    {
        using var db = TestDb.CreateEmpty();
        Should.Throw<System.ArgumentException>(() => db.Customers.OrderByMember("NoSuchProp").ToList());
    }
}
