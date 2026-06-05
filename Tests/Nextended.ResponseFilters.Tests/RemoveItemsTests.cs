using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class RemoveItemsTests
{
    private sealed class DropZeroQty : ResponseFilter<OrderDto>
    {
        public DropZeroQty()
        {
            RemoveItems<LineItemDto>(x => x.Lines)
                .Where(line => line.Quantity == 0)
                .Always();
        }
    }

    private sealed class KeepOnlyHighValue : ResponseFilter<OrderDto>
    {
        public KeepOnlyHighValue()
        {
            KeepOnly<LineItemDto>(x => x.Lines)
                .Where(line => line.UnitCost.GetValueOrDefault() > 50m)
                .Always();
        }
    }

    private sealed class RemoveSensitiveTags : ResponseFilter<OrderDto>
    {
        public RemoveSensitiveTags()
        {
            // Tags is string[] which is IList<string> but readonly under .NET → fallback path
            RemoveItems<string>(x => x.Tags)
                .Where(t => t.StartsWith("internal:"))
                .Always();
        }
    }

    [TestMethod]
    public async Task RemoveItems_InList_MutatesInPlace()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { Sku = "A", Quantity = 0 },
                new() { Sku = "B", Quantity = 5 },
                new() { Sku = "C", Quantity = 0 },
                new() { Sku = "D", Quantity = 3 },
            }
        };

        await new DropZeroQty().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines!.Count.ShouldBe(2);
        order.Lines.ShouldContain(l => l.Sku == "B");
        order.Lines.ShouldContain(l => l.Sku == "D");
    }

    [TestMethod]
    public async Task KeepOnly_InvertedSemantics()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { Sku = "cheap", UnitCost = 10m },
                new() { Sku = "premium", UnitCost = 100m },
                new() { Sku = "mid", UnitCost = 60m },
            }
        };

        await new KeepOnlyHighValue().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines!.Count.ShouldBe(2);
        order.Lines.ShouldAllBe(l => l.UnitCost > 50m);
    }

    [TestMethod]
    public async Task RemoveItems_OnArray_RebuildsAndReassigns()
    {
        // string[] implements IList<string> as readonly → fallback rebuild path
        var order = new OrderDto
        {
            Tags = new[] { "public", "internal:secret", "ok", "internal:audit" }
        };

        await new RemoveSensitiveTags().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Tags.ShouldNotBeNull();
        order.Tags!.Length.ShouldBe(2);
        order.Tags.ShouldContain("public");
        order.Tags.ShouldContain("ok");
    }

    [TestMethod]
    public async Task RemoveItems_NullCollection_IsNoop()
    {
        var order = new OrderDto { Lines = null };
        await new DropZeroQty().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines.ShouldBeNull();
    }
}
