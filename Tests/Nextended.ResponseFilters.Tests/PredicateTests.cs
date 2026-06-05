using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class PredicateTests
{
    private sealed class Unless : ResponseFilter<OrderDto>
    {
        public Unless() => Nullify(x => x.TotalCost).Unless((dto, _) => dto.IsActive);
    }

    private sealed class WhenAllFilter : ResponseFilter<OrderDto>
    {
        public WhenAllFilter() => Nullify(x => x.TotalCost).WhenAll(
            (dto, _) => new System.Threading.Tasks.ValueTask<bool>(dto.Id > 0),
            (dto, _) => new System.Threading.Tasks.ValueTask<bool>(!dto.IsActive));
    }

    private sealed class WhenAnyFilter : ResponseFilter<OrderDto>
    {
        public WhenAnyFilter() => Nullify(x => x.TotalCost).WhenAny(
            (dto, _) => new System.Threading.Tasks.ValueTask<bool>(dto.Id < 0),
            (dto, _) => new System.Threading.Tasks.ValueTask<bool>(!dto.IsActive));
    }

    [TestMethod]
    public async Task Unless_True_DoesNotFire()
    {
        var order = new OrderDto { TotalCost = 1m, IsActive = true };
        await new Unless().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBe(1m);
    }

    [TestMethod]
    public async Task Unless_False_Fires()
    {
        var order = new OrderDto { TotalCost = 1m, IsActive = false };
        await new Unless().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task WhenAll_BothTrue_Fires()
    {
        var order = new OrderDto { Id = 1, IsActive = false, TotalCost = 10m };
        await new WhenAllFilter().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task WhenAll_OneFalse_DoesNotFire()
    {
        var order = new OrderDto { Id = 1, IsActive = true, TotalCost = 10m };
        await new WhenAllFilter().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBe(10m);
    }

    [TestMethod]
    public async Task WhenAny_OneTrue_Fires()
    {
        // Id < 0 false, !IsActive true → fires
        var order = new OrderDto { Id = 1, IsActive = false, TotalCost = 10m };
        await new WhenAnyFilter().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task WhenAny_AllFalse_DoesNotFire()
    {
        var order = new OrderDto { Id = 1, IsActive = true, TotalCost = 10m };
        await new WhenAnyFilter().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBe(10m);
    }
}
