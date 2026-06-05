using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class TransformTests
{
    private sealed class UppercaseEmail : ResponseFilter<OrderDto>
    {
        public UppercaseEmail()
        {
            Transform(x => x.Email).Using(e => e?.ToUpperInvariant()).Always();
        }
    }

    private sealed class CostBasedNotes : ResponseFilter<OrderDto>
    {
        public CostBasedNotes()
        {
            Transform(x => x.Notes).Using((o, _) => $"{o.Notes ?? "-"}|cost={o.TotalCost}").Always();
        }
    }

    [TestMethod]
    public async Task Using_PureFunc_TransformsValue()
    {
        var order = new OrderDto { Email = "hello@host" };
        await new UppercaseEmail().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe("HELLO@HOST");
    }

    [TestMethod]
    public async Task Using_InstanceAware_HasAccessToOtherProperties()
    {
        var order = new OrderDto { Notes = "a", TotalCost = 12m };
        await new CostBasedNotes().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("a|cost=12");
    }
}
