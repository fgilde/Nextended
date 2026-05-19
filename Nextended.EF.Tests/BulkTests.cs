using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.EF.Tests.Domain;
using Nextended.EF.Tests.Support;
using Shouldly;

namespace Nextended.EF.Tests;

[TestClass]
public class BulkTests
{
    [TestMethod]
    public async Task BulkInsertAsync_InsertsAllInBatches()
    {
        await using var db = TestDb.CreateEmpty();

        var products = Enumerable.Range(1, 25)
            .Select(i => new Product { Id = i, Name = $"P-{i}", Price = i })
            .ToList();

        var written = await db.BulkInsertAsync(products, batchSize: 10);

        written.ShouldBe(25);
        (await db.Products.CountAsync()).ShouldBe(25);
    }

    [TestMethod]
    public async Task BulkInsertAsync_EmptyInput_DoesNothing()
    {
        await using var db = TestDb.CreateEmpty();

        var written = await db.BulkInsertAsync<Product>(System.Array.Empty<Product>());
        written.ShouldBe(0);
    }

    [TestMethod]
    public async Task BulkDeleteWhereAsync_RemovesMatchingRows()
    {
        await using var db = await TestDb.SeedAsync();

        var deleted = await db.OrderLines.BulkDeleteWhereAsync(l => l.UnitPrice < 6m);

        deleted.ShouldBeGreaterThanOrEqualTo(1);
        (await db.OrderLines.AnyAsync(l => l.UnitPrice < 6m)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpsertRangeAsync_UpdatesExistingAndInsertsNew()
    {
        await using var db = await TestDb.SeedAsync();

        var incoming = new[]
        {
            new Product { Id = 1, Name = "Widget v2", Price = 12.00m, Sku = "W-1" },     // update
            new Product { Id = 99, Name = "Newcomer", Price = 1.00m, Sku = "N-99" },     // insert
        };

        await db.UpsertRangeAsync(
            incoming,
            keySelector: p => p.Id,
            updateExisting: (existing, src) =>
            {
                existing.Name = src.Name;
                existing.Price = src.Price;
            });

        var widget = await db.Products.AsNoTracking().FirstAsync(p => p.Id == 1);
        widget.Name.ShouldBe("Widget v2");
        widget.Price.ShouldBe(12.00m);

        var newcomer = await db.Products.AsNoTracking().FirstAsync(p => p.Id == 99);
        newcomer.Name.ShouldBe("Newcomer");
    }

    [TestMethod]
    public async Task UpsertAsync_InsertsWhenMissing()
    {
        await using var db = await TestDb.SeedAsync();

        var product = await db.UpsertAsync(
            new Product { Id = 100, Name = "Single", Price = 7m },
            p => p.Id);

        product.Name.ShouldBe("Single");
        (await db.Products.AsNoTracking().AnyAsync(p => p.Id == 100)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpsertAsync_UpdatesWhenPresent()
    {
        await using var db = await TestDb.SeedAsync();

        await db.UpsertAsync(
            new Product { Id = 2, Name = "Gizmo v2", Price = 25m },
            p => p.Id,
            updateExisting: (existing, src) =>
            {
                existing.Name = src.Name;
                existing.Price = src.Price;
            });

        var gizmo = await db.Products.AsNoTracking().FirstAsync(p => p.Id == 2);
        gizmo.Name.ShouldBe("Gizmo v2");
        gizmo.Price.ShouldBe(25m);
    }
}
