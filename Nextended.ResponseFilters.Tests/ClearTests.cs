using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class ClearTests
{
    private sealed class ClearNotes : ResponseFilter<OrderDto>
    {
        public ClearNotes() => Clear(x => x.Notes).Always();
    }

    private sealed class ClearLines : ResponseFilter<OrderDto>
    {
        public ClearLines() => Clear(x => x.Lines).Always();
    }

    private sealed class ClearTags : ResponseFilter<OrderDto>
    {
        public ClearTags() => Clear(x => x.Tags).Always();
    }

    [TestMethod]
    public async Task Clear_StringProperty_SetsToEmpty()
    {
        var order = new OrderDto { Notes = "non-empty notes" };
        await new ClearNotes().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Notes.ShouldBe(string.Empty);
    }

    [TestMethod]
    public async Task Clear_NullString_SetsToEmpty()
    {
        var order = new OrderDto { Notes = null };
        await new ClearNotes().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        // Type-aware fallback: string → ""
        order.Notes.ShouldBe(string.Empty);
    }

    [TestMethod]
    public async Task Clear_MutableList_ClearsInPlace()
    {
        var list = new List<LineItemDto>
        {
            new() { Sku = "a" },
            new() { Sku = "b" },
        };
        var order = new OrderDto { Lines = list };

        await new ClearLines().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Lines.ShouldNotBeNull();
        order.Lines!.Count.ShouldBe(0);
        // In-place mutation: same reference
        order.Lines.ShouldBeSameAs(list);
    }

    [TestMethod]
    public async Task Clear_Array_ReplacesWithEmptyArray()
    {
        var order = new OrderDto { Tags = new[] { "a", "b" } };
        await new ClearTags().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Tags.ShouldNotBeNull();
        order.Tags!.Length.ShouldBe(0);
    }
}
