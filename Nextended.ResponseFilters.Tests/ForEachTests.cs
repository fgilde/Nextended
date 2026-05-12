using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class ForEachTests
{
    private sealed class NullifyLineCosts : ResponseFilter<OrderDto>
    {
        public NullifyLineCosts()
        {
            ForEach(x => x.Lines, line =>
                line.Nullify(l => l.UnitCost).Always());
        }
    }

    private sealed class MaskLineSkus : ResponseFilter<OrderDto>
    {
        public MaskLineSkus()
        {
            ForEach(x => x.Lines, line =>
                line.Mask(l => l.Sku).KeepFirst(2).Always());
        }
    }

    [TestMethod]
    public async Task ForEach_AppliesSubFilterToEachItem()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { UnitCost = 1m },
                new() { UnitCost = 2m },
                new() { UnitCost = 3m },
            }
        };

        await new NullifyLineCosts().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines.ShouldAllBe(l => l.UnitCost == null);
    }

    [TestMethod]
    public async Task ForEach_NullCollection_IsNoop()
    {
        var order = new OrderDto { Lines = null };
        await new NullifyLineCosts().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines.ShouldBeNull();
    }

    [TestMethod]
    public async Task ForEach_AppliesMaskInsideCollection()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto> { new() { Sku = "SKU-12345" } }
        };

        await new MaskLineSkus().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines![0].Sku.ShouldBe("SK*******");
    }
}
