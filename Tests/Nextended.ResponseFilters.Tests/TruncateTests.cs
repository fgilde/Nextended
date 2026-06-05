using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class TruncateTests
{
    private sealed class HardCut : ResponseFilter<OrderDto>
    {
        public HardCut() => Truncate(x => x.Notes).After(5).Always();
    }

    private sealed class WithEllipsis : ResponseFilter<OrderDto>
    {
        public WithEllipsis() => Truncate(x => x.Notes).After(5, "…").Always();
    }

    [TestMethod]
    public async Task LongerThanLimit_GetsCut()
    {
        var order = new OrderDto { Notes = "0123456789" };
        await new HardCut().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("01234");
    }

    [TestMethod]
    public async Task ShorterThanLimit_StaysUnchanged()
    {
        var order = new OrderDto { Notes = "abc" };
        await new HardCut().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("abc");
    }

    [TestMethod]
    public async Task WithSuffix_AppendsWhenTruncated()
    {
        var order = new OrderDto { Notes = "0123456789" };
        await new WithEllipsis().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("01234…");
    }

    [TestMethod]
    public async Task WithSuffix_NoSuffixWhenNotTruncated()
    {
        var order = new OrderDto { Notes = "abc" };
        await new WithEllipsis().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("abc");
    }
}
