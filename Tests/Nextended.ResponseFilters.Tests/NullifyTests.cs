using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class NullifyTests
{
    private sealed class NullifyTotalCost : ResponseFilter<OrderDto>
    {
        public NullifyTotalCost()
        {
            Nullify(x => x.TotalCost).Always();
        }
    }

    private sealed class NullifyMultiple : ResponseFilter<OrderDto>
    {
        public NullifyMultiple()
        {
            Nullify(x => x.TotalCost).Always();
            Nullify(x => x.Email, x => x.Notes).Always();
        }
    }

    private sealed class NullifyWhenInactive : ResponseFilter<OrderDto>
    {
        public NullifyWhenInactive()
        {
            Nullify(x => x.TotalCost).When((dto, _) => !dto.IsActive);
        }
    }

    [TestMethod]
    public async Task Nullify_AlwaysFires_SetsToNull()
    {
        var order = new OrderDto { TotalCost = 99m };
        await new NullifyTotalCost().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task Nullify_MultipleSelectors_NullsAllListedProperties()
    {
        var order = new OrderDto { TotalCost = 1m, Email = "a@b", Notes = "n" };
        await new NullifyMultiple().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
        order.Email.ShouldBeNull();
        order.Notes.ShouldBeNull();
    }

    [TestMethod]
    public async Task Nullify_PredicateFalse_LeavesValueIntact()
    {
        var order = new OrderDto { TotalCost = 50m, IsActive = true };
        await new NullifyWhenInactive().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBe(50m);
    }

    [TestMethod]
    public async Task Nullify_PredicateTrue_NullsValue()
    {
        var order = new OrderDto { TotalCost = 50m, IsActive = false };
        await new NullifyWhenInactive().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public void Nullify_EmptySelectorArray_Throws()
    {
        Should.Throw<System.ArgumentException>(() => new EmptyNullifyFilter());
    }

    private sealed class EmptyNullifyFilter : ResponseFilter<OrderDto>
    {
        public EmptyNullifyFilter()
        {
            Nullify<decimal?>(); // no selectors
        }
    }
}
