using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

/// <summary>
/// Covers the predicate-overload matrix on RuleBuilderBase. Every shape (no-arg / ctx / instance,
/// sync / async) must reach the same canonical AsyncPredicate&lt;T&gt; under the hood.
/// </summary>
[TestClass]
public class PredicateOverloadTests
{
    // ---- When ---------------------------------------------------------------

    private sealed class WhenNoArgSync : ResponseFilter<OrderDto>
    {
        public WhenNoArgSync(Func<bool> p) => Nullify(x => x.TotalCost).When(p);
    }

    private sealed class WhenNoArgAsync : ResponseFilter<OrderDto>
    {
        public WhenNoArgAsync(Func<Task<bool>> p) => Nullify(x => x.TotalCost).When(p);
    }

    private sealed class WhenCtxSync : ResponseFilter<OrderDto>
    {
        public WhenCtxSync(Func<IResponseFilterContext, bool> p) => Nullify(x => x.TotalCost).When(p);
    }

    private sealed class WhenCtxAsync : ResponseFilter<OrderDto>
    {
        public WhenCtxAsync(Func<IResponseFilterContext, Task<bool>> p) => Nullify(x => x.TotalCost).When(p);
    }

    private sealed class WhenInstanceSync : ResponseFilter<OrderDto>
    {
        public WhenInstanceSync(Func<OrderDto, bool> p) => Nullify(x => x.TotalCost).When(p);
    }

    private sealed class WhenInstanceAsync : ResponseFilter<OrderDto>
    {
        public WhenInstanceAsync(Func<OrderDto, Task<bool>> p) => Nullify(x => x.TotalCost).When(p);
    }

    [TestMethod]
    public async Task When_NoArgSync_True_Fires()
    {
        var order = new OrderDto { TotalCost = 1m };
        await new WhenNoArgSync(() => true).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_NoArgAsync_True_Fires()
    {
        var order = new OrderDto { TotalCost = 1m };
        await new WhenNoArgAsync(() => Task.FromResult(true)).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_CtxSync_ReadsContextItems()
    {
        var order = new OrderDto { TotalCost = 1m };
        var ctx = Helpers.MakeContext();
        ctx.Items["fire"] = true;

        await new WhenCtxSync(c => (bool)c.Items["fire"]!).ApplyAsync(order, ctx).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_CtxAsync_AwaitsAsyncSource()
    {
        var order = new OrderDto { TotalCost = 1m };
        await new WhenCtxAsync(async c =>
        {
            await Task.Yield();
            return true;
        }).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_InstanceSync_True_Fires()
    {
        var order = new OrderDto { TotalCost = 1m, Id = 5 };
        await new WhenInstanceSync(o => o.Id == 5).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_InstanceAsync_True_Fires()
    {
        var order = new OrderDto { TotalCost = 1m, Id = 5 };
        await new WhenInstanceAsync(async o =>
        {
            await Task.Yield();
            return o.Id > 0;
        }).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    // ---- Unless mirror -------------------------------------------------------

    private sealed class UnlessNoArgSync : ResponseFilter<OrderDto>
    {
        public UnlessNoArgSync(Func<bool> p) => Nullify(x => x.TotalCost).Unless(p);
    }

    private sealed class UnlessCtxAsync : ResponseFilter<OrderDto>
    {
        public UnlessCtxAsync(Func<IResponseFilterContext, Task<bool>> p) => Nullify(x => x.TotalCost).Unless(p);
    }

    private sealed class UnlessInstanceSync : ResponseFilter<OrderDto>
    {
        public UnlessInstanceSync(Func<OrderDto, bool> p) => Nullify(x => x.TotalCost).Unless(p);
    }

    [TestMethod]
    public async Task Unless_NoArgSync_False_Fires()
    {
        var order = new OrderDto { TotalCost = 1m };
        await new UnlessNoArgSync(() => false).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task Unless_CtxAsync_False_Fires()
    {
        var order = new OrderDto { TotalCost = 1m };
        await new UnlessCtxAsync(_ => Task.FromResult(false)).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task Unless_InstanceSync_False_Fires()
    {
        var order = new OrderDto { TotalCost = 1m, IsActive = false };
        await new UnlessInstanceSync(o => o.IsActive).ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    // ---- Realistic permission-style helper test -----------------------------

    private sealed class PermStub
    {
        public bool Granted { get; set; }
        public Task<bool> IsGrantedAsync(string policy) => Task.FromResult(Granted);
    }

    private static Func<IResponseFilterContext, Task<bool>> Missing(string policy)
        => async ctx => !await ctx.Services.GetRequiredService<PermStub>().IsGrantedAsync(policy).ConfigureAwait(false);

    private sealed class NullifyWhenMissing : ResponseFilter<OrderDto>
    {
        public NullifyWhenMissing() => Nullify(x => x.TotalCost).When(Missing("Finance.View"));
    }

    [TestMethod]
    public async Task RealWorld_CtxAsyncPredicate_ResolvedFromServices()
    {
        var stub = new PermStub { Granted = false };
        var ctx = Helpers.MakeContext(s => s.AddSingleton(stub));
        var order = new OrderDto { TotalCost = 99m };

        await new NullifyWhenMissing().ApplyAsync(order, ctx).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task RealWorld_PermissionGranted_DoesNotFire()
    {
        var stub = new PermStub { Granted = true };
        var ctx = Helpers.MakeContext(s => s.AddSingleton(stub));
        var order = new OrderDto { TotalCost = 99m };

        await new NullifyWhenMissing().ApplyAsync(order, ctx).ConfigureAwait(false);

        order.TotalCost.ShouldBe(99m);
    }
}
