using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class SetValueTests
{
    private sealed class SetConstant : ResponseFilter<OrderDto>
    {
        public SetConstant()
        {
            SetValue(x => x.Notes).To("REDACTED").Always();
        }
    }

    private sealed class SetFromInstance : ResponseFilter<OrderDto>
    {
        public SetFromInstance()
        {
            SetValue(x => x.Notes).To(o => $"order-{o.Id}").Always();
        }
    }

    private sealed class SetFromContext : ResponseFilter<OrderDto>
    {
        public SetFromContext()
        {
            SetValue(x => x.Notes).To((_, ctx) => (string?)ctx.Items["override"]).Always();
        }
    }

    [TestMethod]
    public async Task To_Constant_SetsValue()
    {
        var order = new OrderDto();
        await new SetConstant().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("REDACTED");
    }

    [TestMethod]
    public async Task To_Factory_UsesInstanceData()
    {
        var order = new OrderDto { Id = 42 };
        await new SetFromInstance().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("order-42");
    }

    [TestMethod]
    public async Task To_FactoryWithContext_ReadsItemsBag()
    {
        var order = new OrderDto();
        var ctx = Helpers.MakeContext();
        ctx.Items["override"] = "from-ctx";

        await new SetFromContext().ApplyAsync(order, ctx).ConfigureAwait(false);

        order.Notes.ShouldBe("from-ctx");
    }
}
