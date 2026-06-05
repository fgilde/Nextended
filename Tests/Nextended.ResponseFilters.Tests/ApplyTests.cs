using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class ApplyTests
{
    private sealed class SyncApply : ResponseFilter<OrderDto>
    {
        public SyncApply() => Apply((o, _) =>
        {
            o.Notes = "stamped";
            o.IsActive = false;
        }).Always();
    }

    private sealed class AsyncApply : ResponseFilter<OrderDto>
    {
        public AsyncApply() => ApplyAsync(async (o, _) =>
        {
            await Task.Yield();
            o.Notes = "async-stamped";
        }).Always();
    }

    [TestMethod]
    public async Task SyncApply_MutatesInstance()
    {
        var order = new OrderDto { IsActive = true };
        await new SyncApply().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("stamped");
        order.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AsyncApply_AwaitsAction()
    {
        var order = new OrderDto();
        await new AsyncApply().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe("async-stamped");
    }
}
