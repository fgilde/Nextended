using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class ReplaceTests
{
    private sealed class ReplaceConstant : ResponseFilter<OrderDto>
    {
        public ReplaceConstant()
        {
            Replace(x => x.Email).With("***").Always();
        }
    }

    private sealed class ReplaceFromInstance : ResponseFilter<OrderDto>
    {
        public ReplaceFromInstance()
        {
            Replace(x => x.Email).With(o => $"id-{o.Id}@redacted").Always();
        }
    }

    [TestMethod]
    public async Task With_Constant_Replaces()
    {
        var order = new OrderDto { Email = "user@host.tld" };
        await new ReplaceConstant().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe("***");
    }

    [TestMethod]
    public async Task With_Factory_UsesInstanceData()
    {
        var order = new OrderDto { Id = 7, Email = "x" };
        await new ReplaceFromInstance().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe("id-7@redacted");
    }
}
