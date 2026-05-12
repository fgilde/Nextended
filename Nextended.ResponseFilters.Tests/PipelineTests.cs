using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class PipelineTests
{
    private sealed class OrderFilter : ResponseFilter<OrderDto>
    {
        public OrderFilter() => Nullify(x => x.TotalCost).Always();
    }

    private sealed class CustomerFilter : ResponseFilter<CustomerDto>
    {
        public CustomerFilter() => Nullify(x => x.CreditLimit).Always();
    }

    private sealed class LineFilter : ResponseFilter<LineItemDto>
    {
        public LineFilter() => Nullify(x => x.UnitCost).Always();
    }

    [TestMethod]
    public async Task Pipeline_AppliesFilterForRootType()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(new OrderFilter());
        var order = new OrderDto { TotalCost = 99m };

        await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task Pipeline_RecursesIntoNestedProperty()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(new OrderFilter(), new CustomerFilter());
        var order = new OrderDto
        {
            TotalCost = 10m,
            Customer = new CustomerDto { CreditLimit = 500m }
        };

        await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
        order.Customer!.CreditLimit.ShouldBeNull();
    }

    [TestMethod]
    public async Task Pipeline_RecursesIntoCollection()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(new LineFilter());
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { UnitCost = 1m },
                new() { UnitCost = 2m },
            }
        };

        await pipeline.ProcessAsync(order, ctx).ConfigureAwait(false);

        order.Lines.ShouldAllBe(l => l.UnitCost == null);
    }

    [TestMethod]
    public async Task Pipeline_TopLevelArray_FiltersEachElement()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(new OrderFilter());
        var array = new[]
        {
            new OrderDto { TotalCost = 1m },
            new OrderDto { TotalCost = 2m },
        };

        await pipeline.ProcessAsync(array, ctx).ConfigureAwait(false);

        array[0].TotalCost.ShouldBeNull();
        array[1].TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task Pipeline_CycleDetection_DoesNotInfinitelyRecurse()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline();
        var a = new CyclicDto { Name = "a" };
        a.Self = a; // cycle

        // Should return quickly without StackOverflow
        await pipeline.ProcessAsync(a, ctx).ConfigureAwait(false);

        a.Name.ShouldBe("a"); // no filter registered, value unchanged
    }

    [TestMethod]
    public async Task Pipeline_NullRoot_IsNoop()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline(new OrderFilter());

        await pipeline.ProcessAsync(null, ctx).ConfigureAwait(false);
        // (no exception means success)
    }

    [TestMethod]
    public async Task Pipeline_IndexerProperty_DoesNotCrashOnRecursion()
    {
        var (pipeline, ctx) = Helpers.BuildPipeline();
        var dto = new IndexerDto { Name = "x" };
        dto["key"] = "value";

        // The recursion must skip the indexer (which has GetIndexParameters().Length > 0)
        await pipeline.ProcessAsync(dto, ctx).ConfigureAwait(false);

        dto.Name.ShouldBe("x");
    }
}
