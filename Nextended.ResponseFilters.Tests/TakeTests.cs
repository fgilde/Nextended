using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class TakeTests
{
    private sealed class TakeFirst2 : ResponseFilter<OrderDto>
    {
        public TakeFirst2() => Take<LineItemDto>(x => x.Lines).First(2).Always();
    }

    private sealed class TakeLast1 : ResponseFilter<OrderDto>
    {
        public TakeLast1() => Take<LineItemDto>(x => x.Lines).Last(1).Always();
    }

    [TestMethod]
    public async Task TakeFirst_TrimsToN()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { Sku = "A" },
                new() { Sku = "B" },
                new() { Sku = "C" },
                new() { Sku = "D" },
            }
        };

        await new TakeFirst2().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines!.Count.ShouldBe(2);
        order.Lines[0].Sku.ShouldBe("A");
        order.Lines[1].Sku.ShouldBe("B");
    }

    [TestMethod]
    public async Task TakeLast_KeepsTailItems()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { Sku = "A" },
                new() { Sku = "B" },
                new() { Sku = "C" },
            }
        };

        await new TakeLast1().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines!.Count.ShouldBe(1);
        order.Lines[0].Sku.ShouldBe("C");
    }

    [TestMethod]
    public async Task Take_FewerItemsThanLimit_IsNoop()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto> { new() { Sku = "A" } }
        };

        await new TakeFirst2().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines!.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Take_NullCollection_IsNoop()
    {
        var order = new OrderDto { Lines = null };
        await new TakeFirst2().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines.ShouldBeNull();
    }
}
