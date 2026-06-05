using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Pipeline;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class OptionsAndPipelineBehaviorTests
{
    // -- Filters that throw or get cancelled -------------------------------------

    private sealed class ThrowingFilter : ResponseFilter<OrderDto>
    {
        public ThrowingFilter() => Apply((_, _) => throw new InvalidOperationException("boom")).Always();
    }

    private sealed class CancellingFilter : ResponseFilter<OrderDto>
    {
        public CancellingFilter() => ApplyAsync(async (_, ctx) =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
        }).Always();
    }

    private sealed class NullifyCost : ResponseFilter<OrderDto>
    {
        public NullifyCost() => Nullify(x => x.TotalCost).Always();
    }

    // -- Exception behaviour ------------------------------------------------------

    [TestMethod]
    public async Task Rethrow_Default_ExceptionPropagatesToCaller()
    {
        // Default options → Rethrow
        var (pipeline, ctx) = Helpers.BuildPipeline(new ThrowingFilter());
        var order = new OrderDto();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task LogAndContinue_SwallowsExceptionAndContinues()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(
            configureOptions: o => o.ExceptionBehavior = FilterExceptionBehavior.LogAndContinue,
            filters: new IResponseFilter[] { new ThrowingFilter(), new NullifyCost() });

        var order = new OrderDto { TotalCost = 99m };

        // Should not throw; subsequent filter still runs.
        await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false);
        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task OperationCanceledException_AlwaysPropagates_EvenInLogAndContinue()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var (pipeline, _) = Helpers.BuildPipeline(
            configureOptions: o => o.ExceptionBehavior = FilterExceptionBehavior.LogAndContinue,
            filters: new IResponseFilter[] { new CancellingFilter() });

        // Build a fresh context with the cancelled token
        var services = new ServiceCollection().BuildServiceProvider();
        var ctx = new ResponseFilterContext(services, cts.Token);
        var order = new OrderDto();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false));
    }

    // -- Reachability / skip ------------------------------------------------------

    private sealed class UnrelatedDto
    {
        public string? Name { get; set; }
    }

    [TestMethod]
    public async Task SkipUnaffectedResponses_RootNotInGraph_PipelineDoesNothing()
    {
        // Filter registered for OrderDto, but we feed it an UnrelatedDto
        var (pipeline, ctx) = Helpers.BuildPipeline(new NullifyCost());
        var unrelated = new UnrelatedDto { Name = "still here" };

        await pipeline.ProcessAsync(unrelated, ctx).ConfigureAwait(false);

        unrelated.Name.ShouldBe("still here");
    }

    [TestMethod]
    public async Task SkipUnaffectedResponses_False_PipelineWalksAnyway()
    {
        // With SkipUnaffectedResponses=false the walker still descends — but since no filter is
        // registered for UnrelatedDto, nothing changes. We're only verifying the path runs.
        var (pipeline, ctx) = Helpers.BuildPipeline(
            configureOptions: o => o.SkipUnaffectedResponses = false,
            filters: new IResponseFilter[] { new NullifyCost() });

        var unrelated = new UnrelatedDto { Name = "still here" };
        await pipeline.ProcessAsync(unrelated, ctx).ConfigureAwait(false);

        unrelated.Name.ShouldBe("still here");
    }

    [TestMethod]
    public async Task SkipResponseType_OptOutPredicate_PreventsWalk()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(
            configureOptions: o => o.SkipResponseType = t => t == typeof(OrderDto),
            filters: new IResponseFilter[] { new NullifyCost() });

        // Predicate says "skip OrderDto" — so even though a filter would otherwise fire, it doesn't.
        var order = new OrderDto { TotalCost = 99m };
        await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false);

        order.TotalCost.ShouldBe(99m);
    }

    [TestMethod]
    public async Task Reachability_NestedFilterableType_StillTriggersWalk()
    {
        // Root has no direct filter, but a nested property's type does.
        var (pipeline, ctx) = Helpers.BuildPipeline(new NullifyCost());

        // Wrap an OrderDto inside a holder that has no filter of its own.
        var holder = new HolderDto { Inner = new OrderDto { TotalCost = 99m } };
        await pipeline.ProcessAsync(holder, ctx).ConfigureAwait(false);

        holder.Inner!.TotalCost.ShouldBeNull();
    }

    private sealed class HolderDto
    {
        public OrderDto? Inner { get; set; }
    }

    [TestMethod]
    public void TypeReachabilityCache_NoFiltersRegistered_AlwaysReturnsFalse()
    {
        var cache = new TypeReachabilityCache();
        cache.MayBeAffected(typeof(OrderDto)).ShouldBeFalse();
    }

    [TestMethod]
    public void TypeReachabilityCache_DirectTarget_ReturnsTrue()
    {
        var cache = new TypeReachabilityCache();
        cache.SetTargetTypes(new[] { typeof(OrderDto) });
        cache.MayBeAffected(typeof(OrderDto)).ShouldBeTrue();
    }

    [TestMethod]
    public void TypeReachabilityCache_TransitiveTarget_ReturnsTrue()
    {
        var cache = new TypeReachabilityCache();
        cache.SetTargetTypes(new[] { typeof(LineItemDto) });
        // OrderDto has a property List<LineItemDto> → reachable
        cache.MayBeAffected(typeof(OrderDto)).ShouldBeTrue();
    }

    [TestMethod]
    public void TypeReachabilityCache_NoIntersection_ReturnsFalse()
    {
        var cache = new TypeReachabilityCache();
        cache.SetTargetTypes(new[] { typeof(OrderDto) });
        cache.MayBeAffected(typeof(UnrelatedDto)).ShouldBeFalse();
    }
}
