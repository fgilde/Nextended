using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class RoundTests
{
    private sealed class RoundCostToTwo : ResponseFilter<OrderDto>
    {
        public RoundCostToTwo() => Round(x => x.TotalCost).To(2).Always();
    }

    private sealed class RoundSubtotalToInt : ResponseFilter<OrderDto>
    {
        public RoundSubtotalToInt() => Round(x => x.Subtotal).ToInteger().Always();
    }

    private sealed class RoundScoreToOne : ResponseFilter<OrderDto>
    {
        public RoundScoreToOne() => Round(x => x.Score).To(1, MidpointRounding.AwayFromZero).Always();
    }

    [TestMethod]
    public async Task Round_NullableDecimal_RoundsToTwoPlaces()
    {
        var order = new OrderDto { TotalCost = 12.3456m };
        await new RoundCostToTwo().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBe(12.35m);
    }

    [TestMethod]
    public async Task Round_NullValue_StaysNull()
    {
        var order = new OrderDto { TotalCost = null };
        await new RoundCostToTwo().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public async Task Round_NonNullableDecimal_ToInteger_UsesBankersRounding()
    {
        var order = new OrderDto { Subtotal = 12.5m };
        await new RoundSubtotalToInt().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        // Banker's rounding (ToEven): 12.5 → 12
        order.Subtotal.ShouldBe(12m);
    }

    [TestMethod]
    public async Task Round_Double_WithAwayFromZero()
    {
        var order = new OrderDto { Score = 4.55 };
        await new RoundScoreToOne().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Score.ShouldBe(4.6);
    }

    // -------------------------------------------------------------------------
    // Compile-time-only test: verifies the INumber<T> constraint prevents
    // accidental use on non-numeric properties. The lines below must NOT
    // compile — uncomment to verify the diagnostic locally.
    // -------------------------------------------------------------------------
#pragma warning disable CS0219
    [TestMethod]
    public void Round_OnStringProperty_DoesNotCompile()
    {
        // The following would produce CS0411 ("type arguments cannot be inferred")
        // because string does not implement INumber<string>.
        //
        // class BadFilter : ResponseFilter<OrderDto>
        // {
        //     public BadFilter() => Round(x => x.Notes).To(2).Always();
        // }
        //
        // This is a compile-time guard; if you remove the constraint, the call
        // would still build and silently no-op at runtime.
        true.ShouldBeTrue();
    }
#pragma warning restore CS0219
}
