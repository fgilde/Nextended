using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Json;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class StructuralEditTests
{
    private sealed class RemoveEmail : ResponseFilter<OrderDto>
    {
        public RemoveEmail() => Remove(x => x.Email, x => x.Notes).Always();
    }

    private sealed class RenameId : ResponseFilter<OrderDto>
    {
        public RenameId() => Rename(x => x.Id).To("orderId").Always();
    }

    private sealed class TransformKeys : ResponseFilter<OrderDto>
    {
        public TransformKeys() => TransformKey(x => x.Id).Using(k => "x_" + k).Always();
    }

    private sealed class AddComputed : ResponseFilter<OrderDto>
    {
        public AddComputed() => AddProperty("displayName").From(o => $"#{o.Id}").Always();
    }

    private sealed class ConditionalRemove : ResponseFilter<OrderDto>
    {
        public ConditionalRemove() => Remove(x => x.TotalCost).When((o, _) => !o.IsActive);
    }

    private sealed class RemoveLineCost : ResponseFilter<OrderDto>
    {
        public RemoveLineCost()
            => ForEach(x => x.Lines, line => line.Remove(l => l.UnitCost).Always());
    }

    private static async Task<JsonObject> RunAndTransform(
        IResponseFilter filter, OrderDto order, JsonSerializerOptions? options = null)
    {
        var context = Helpers.MakeContext();
        await filter.ApplyAsync(order, context).ConfigureAwait(false);
        var node = JsonStructuralTransformer.Transform(order, context.StructuralEdits, options);
        return node!.AsObject();
    }

    [TestMethod]
    public async Task Remove_DropsKeysEntirely()
    {
        var json = await RunAndTransform(new RemoveEmail(), new OrderDto { Email = "a@b", Notes = "n" });

        json.ContainsKey("Email").ShouldBeFalse();
        json.ContainsKey("Notes").ShouldBeFalse();
        json.ContainsKey("Id").ShouldBeTrue();
    }

    [TestMethod]
    public async Task Rename_MovesValueToNewKey()
    {
        var json = await RunAndTransform(new RenameId(), new OrderDto { Id = 42 });

        json.ContainsKey("Id").ShouldBeFalse();
        json["orderId"]!.GetValue<int>().ShouldBe(42);
    }

    [TestMethod]
    public async Task TransformKey_RewritesKey()
    {
        var json = await RunAndTransform(new TransformKeys(), new OrderDto { Id = 7 });

        json.ContainsKey("Id").ShouldBeFalse();
        json["x_Id"]!.GetValue<int>().ShouldBe(7);
    }

    [TestMethod]
    public async Task AddProperty_InjectsComputedKey()
    {
        var json = await RunAndTransform(new AddComputed(), new OrderDto { Id = 5 });

        json["displayName"]!.GetValue<string>().ShouldBe("#5");
    }

    [TestMethod]
    public async Task Remove_HonoursNamingPolicy()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = await RunAndTransform(new RemoveEmail(), new OrderDto { Email = "a@b", Notes = "n" }, options);

        // Keys are camelCased; the edit still resolves via the CLR member.
        json.ContainsKey("email").ShouldBeFalse();
        json.ContainsKey("notes").ShouldBeFalse();
        json.ContainsKey("id").ShouldBeTrue();
    }

    [TestMethod]
    public async Task ConditionalRemove_PredicateFalse_KeepsKey()
    {
        var json = await RunAndTransform(new ConditionalRemove(), new OrderDto { TotalCost = 9m, IsActive = true });
        json.ContainsKey("TotalCost").ShouldBeTrue();
    }

    [TestMethod]
    public async Task ConditionalRemove_PredicateTrue_DropsKey()
    {
        var json = await RunAndTransform(new ConditionalRemove(), new OrderDto { TotalCost = 9m, IsActive = false });
        json.ContainsKey("TotalCost").ShouldBeFalse();
    }

    [TestMethod]
    public async Task NoEdits_ReturnsFaithfulSerialization()
    {
        // Empty book → transform is a plain SerializeToNode, nothing dropped.
        var context = Helpers.MakeContext();
        var node = JsonStructuralTransformer.Transform(new OrderDto { Id = 1 }, context.StructuralEdits);
        node!.AsObject().ContainsKey("Id").ShouldBeTrue();
    }

    [TestMethod]
    public async Task Remove_InsideForEach_DropsKeyOnEachItem()
    {
        var order = new OrderDto
        {
            Lines = new List<LineItemDto>
            {
                new() { Sku = "A", UnitCost = 1m },
                new() { Sku = "B", UnitCost = 2m },
            }
        };

        var (pipeline, context) = Helpers.BuildPipeline(new RemoveLineCost());
        await pipeline.ProcessAsync(order, context).ConfigureAwait(false);

        var json = JsonStructuralTransformer.Transform(order, context.StructuralEdits)!.AsObject();
        var lines = json["Lines"]!.AsArray();
        foreach (var line in lines)
        {
            line!.AsObject().ContainsKey("UnitCost").ShouldBeFalse();
            line.AsObject().ContainsKey("Sku").ShouldBeTrue();
        }
    }
}
