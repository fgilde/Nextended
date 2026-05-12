using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class SetToDefaultTests
{
    private sealed class DefaultEverything : ResponseFilter<OrderDto>
    {
        public DefaultEverything()
        {
            // Mixed property types: nullable decimal, non-nullable decimal, non-nullable bool, reference type
            SetToDefault(
                x => x.TotalCost,
                x => x.Subtotal,
                x => x.IsActive,
                x => x.Notes
            ).Always();
        }
    }

    [TestMethod]
    public async Task SetToDefault_NullsReferenceTypes()
    {
        var order = new OrderDto { Notes = "x" };
        await new DefaultEverything().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBeNull();
    }

    [TestMethod]
    public async Task SetToDefault_NullsNullableValueTypes()
    {
        var order = new OrderDto { TotalCost = 99m };
        await new DefaultEverything().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task SetToDefault_ZeroesNonNullableValueTypes()
    {
        var order = new OrderDto { Subtotal = 123.45m, IsActive = true };
        await new DefaultEverything().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Subtotal.ShouldBe(0m);
        order.IsActive.ShouldBeFalse();
    }
}
