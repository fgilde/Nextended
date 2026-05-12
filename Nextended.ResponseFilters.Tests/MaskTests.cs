using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class MaskTests
{
    private sealed class FullMask : ResponseFilter<OrderDto>
    {
        public FullMask()
        {
            Mask(x => x.Email).Always();
        }
    }

    private sealed class PartialMask : ResponseFilter<OrderDto>
    {
        public PartialMask()
        {
            Mask(x => x.CreditCard).KeepFirst(4).KeepLast(4).With('#').Always();
        }
    }

    private sealed class PatternMask : ResponseFilter<OrderDto>
    {
        public PatternMask()
        {
            Mask(x => x.Email).WithPattern("***@***.***").Always();
        }
    }

    [TestMethod]
    public async Task FullMask_ReplacesEachCharWithStar()
    {
        var order = new OrderDto { Email = "abc" };
        await new FullMask().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe("***");
    }

    [TestMethod]
    public async Task PartialMask_KeepFirstAndLast()
    {
        var order = new OrderDto { CreditCard = "1234567812345678" };
        await new PartialMask().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.CreditCard.ShouldBe("1234########5678");
    }

    [TestMethod]
    public async Task PatternMask_ReplacesWholeValue()
    {
        var order = new OrderDto { Email = "user@host.tld" };
        await new PatternMask().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe("***@***.***");
    }

    [TestMethod]
    public async Task Mask_NullValue_StaysNull()
    {
        var order = new OrderDto { Email = null };
        await new FullMask().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBeNull();
    }

    [TestMethod]
    public async Task Mask_EmptyString_StaysEmpty()
    {
        var order = new OrderDto { Email = string.Empty };
        await new FullMask().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe(string.Empty);
    }

    private sealed class KeepLongerThanString : ResponseFilter<OrderDto>
    {
        public KeepLongerThanString() =>
            Mask(x => x.CreditCard).KeepFirst(3).KeepLast(3).Always();
    }

    [TestMethod]
    public async Task Mask_KeepsLongerThanString_ReturnsOriginal()
    {
        var order = new OrderDto { CreditCard = "ab" };
        await new KeepLongerThanString().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        // Both keeps exceed length → string is returned unchanged
        order.CreditCard.ShouldBe("ab");
    }
}
