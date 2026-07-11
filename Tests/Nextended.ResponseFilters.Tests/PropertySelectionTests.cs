using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Json;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class PropertySelectionTests
{
    [AttributeUsage(AttributeTargets.Property)]
    private sealed class SecretAttribute : Attribute { }

    private sealed class Dto
    {
        public string? Public { get; set; }
        [Secret] public string? Secret1 { get; set; }
        [Secret] public string? Secret2 { get; set; }
        public int Count { get; set; }
    }

    // ── WhenProperty ────────────────────────────────────────────────────

    private sealed class NullifyOnlySecret1 : ResponseFilter<Dto>
    {
        public NullifyOnlySecret1()
            => Nullify(x => x.Secret1, x => x.Secret2).WhenProperty(p => p.Name == "Secret1").Always();
    }

    [TestMethod]
    public async Task WhenProperty_FiltersToMatchingAccessorsOnly()
    {
        var dto = new Dto { Secret1 = "a", Secret2 = "b" };
        await new NullifyOnlySecret1().ApplyAsync(dto, Helpers.MakeContext()).ConfigureAwait(false);

        dto.Secret1.ShouldBeNull();
        dto.Secret2.ShouldBe("b");
    }

    private sealed class NullifySecretsByAttribute : ResponseFilter<Dto>
    {
        public NullifySecretsByAttribute()
            => Nullify(x => x.Public, x => x.Secret1, x => x.Secret2)
                .WhenProperty(p => p.GetCustomAttribute<SecretAttribute>() != null).Always();
    }

    [TestMethod]
    public async Task WhenProperty_ByAttribute_NullsOnlyDecorated()
    {
        var dto = new Dto { Public = "keep", Secret1 = "a", Secret2 = "b" };
        await new NullifySecretsByAttribute().ApplyAsync(dto, Helpers.MakeContext()).ConfigureAwait(false);

        dto.Public.ShouldBe("keep");
        dto.Secret1.ShouldBeNull();
        dto.Secret2.ShouldBeNull();
    }

    private sealed class MaskGatedOut : ResponseFilter<Dto>
    {
        public MaskGatedOut()
            => Mask(x => x.Public).KeepFirst(1).WhenProperty(_ => false).Always();
    }

    [TestMethod]
    public async Task WhenProperty_FalseOnSingleProperty_IsNoop()
    {
        var dto = new Dto { Public = "secret" };
        await new MaskGatedOut().ApplyAsync(dto, Helpers.MakeContext()).ConfigureAwait(false);

        dto.Public.ShouldBe("secret");
    }

    private sealed class RemoveSecret1ByProperty : ResponseFilter<Dto>
    {
        public RemoveSecret1ByProperty()
            => Remove(x => x.Secret1, x => x.Secret2).WhenProperty(p => p.Name == "Secret1").Always();
    }

    [TestMethod]
    public async Task WhenProperty_OnRemove_DropsOnlyMatchingKey()
    {
        var dto = new Dto { Secret1 = "a", Secret2 = "b" };
        var ctx = Helpers.MakeContext();
        await new RemoveSecret1ByProperty().ApplyAsync(dto, ctx).ConfigureAwait(false);

        var json = JsonStructuralTransformer.Transform(dto, ctx.StructuralEdits)!.AsObject();
        json.ContainsKey("Secret1").ShouldBeFalse();
        json.ContainsKey("Secret2").ShouldBeTrue();
    }

    // ── Properties / PropertiesWhere ────────────────────────────────────

    private sealed class RemoveSecretsBulk : ResponseFilter<Dto>
    {
        public RemoveSecretsBulk()
            => PropertiesWhere(p => p.GetCustomAttribute<SecretAttribute>() != null).Remove().Always();
    }

    [TestMethod]
    public async Task PropertiesWhere_Remove_DropsAllMatching()
    {
        var dto = new Dto { Public = "p", Secret1 = "a", Secret2 = "b", Count = 3 };
        var ctx = Helpers.MakeContext();
        await new RemoveSecretsBulk().ApplyAsync(dto, ctx).ConfigureAwait(false);

        var json = JsonStructuralTransformer.Transform(dto, ctx.StructuralEdits)!.AsObject();
        json.ContainsKey("Secret1").ShouldBeFalse();
        json.ContainsKey("Secret2").ShouldBeFalse();
        json.ContainsKey("Public").ShouldBeTrue();
        json.ContainsKey("Count").ShouldBeTrue();
    }

    private sealed class NullifyNamedSet : ResponseFilter<Dto>
    {
        public NullifyNamedSet()
            => Properties(x => x.Public, x => x.Secret1).Nullify().Always();
    }

    [TestMethod]
    public async Task Properties_Nullify_NullsListedOnly()
    {
        var dto = new Dto { Public = "p", Secret1 = "a", Secret2 = "b" };
        await new NullifyNamedSet().ApplyAsync(dto, Helpers.MakeContext()).ConfigureAwait(false);

        dto.Public.ShouldBeNull();
        dto.Secret1.ShouldBeNull();
        dto.Secret2.ShouldBe("b");
    }

    private sealed class TransformNamedKeys : ResponseFilter<Dto>
    {
        public TransformNamedKeys()
            => Properties(x => x.Public, x => x.Count).TransformKey().Using(k => "x_" + k).Always();
    }

    [TestMethod]
    public async Task Properties_TransformKey_RewritesSelectedKeys()
    {
        var dto = new Dto { Public = "p", Count = 3, Secret1 = "s" };
        var ctx = Helpers.MakeContext();
        await new TransformNamedKeys().ApplyAsync(dto, ctx).ConfigureAwait(false);

        var json = JsonStructuralTransformer.Transform(dto, ctx.StructuralEdits)!.AsObject();
        json.ContainsKey("x_Public").ShouldBeTrue();
        json.ContainsKey("x_Count").ShouldBeTrue();
        json.ContainsKey("Public").ShouldBeFalse();
        json.ContainsKey("Secret1").ShouldBeTrue(); // not selected → unchanged
    }
}
