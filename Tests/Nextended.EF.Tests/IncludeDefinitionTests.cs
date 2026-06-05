using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.IncludeDefinitions;
using Nextended.EF.Tests.Domain;
using Shouldly;

namespace Nextended.EF.Tests;

[TestClass]
public class IncludeDefinitionTests
{
    [TestMethod]
    public void Include_ByExpression_AddsSinglePath()
    {
        var def = new IncludeDefinitionFor<Customer>()
            .Include(c => c.Address);

        def.GetPaths().ShouldBe(new[] { "Address" });
    }

    [TestMethod]
    public void Include_MultipleExpressions_AddsAllAndDedupes()
    {
        var def = new IncludeDefinitionFor<Customer>()
            .Include(c => c.Address)
            .Include(c => c.Orders)
            .Include(c => c.Address);

        def.GetPaths().OrderBy(p => p).ShouldBe(new[] { "Address", "Orders" });
    }

    [TestMethod]
    public void Include_NestedPropertyPath_IsCaptured()
    {
        var def = new IncludeDefinitionFor<Order>()
            .Include(o => o.Customer!.Address);

        def.GetPaths().ShouldContain("Customer.Address");
    }

    [TestMethod]
    public void IncludeWithPrefix_PrependsPathFromOtherDefinition()
    {
        var inner = new IncludePathDefinition().Include("Lines", "Lines.Product");
        var def = new IncludeDefinitionFor<Customer>()
            .Include(c => c.Address)
            .IncludeWithPrefix<Customer, Order>(c => c.Orders, inner);

        var paths = def.GetPaths().ToArray();
        paths.ShouldContain("Address");
        paths.ShouldContain("Orders.Lines");
        paths.ShouldContain("Orders.Lines.Product");
    }

    [TestMethod]
    public void IncludeWithPrefix_WithMutate_AppliesMutation()
    {
        var inner = new IncludePathDefinition().Include("Lines", "Lines.Product", "Customer");
        var def = new IncludeDefinitionFor<Customer>()
            .IncludeWithPrefix<Customer, Order>(
                c => c.Orders,
                inner,
                d => d.WithoutPrefix("Customer"));

        var paths = def.GetPaths().ToArray();
        paths.ShouldContain("Orders.Lines");
        paths.ShouldContain("Orders.Lines.Product");
        paths.ShouldNotContain("Orders.Customer");
    }

    [TestMethod]
    public void IncludeAllVirtual_DiscoversVirtualNavigations()
    {
        var def = new IncludeDefinitionFor<Customer>().IncludeAllVirtual(maxDepth: 2);

        var paths = def.GetPaths().ToArray();
        paths.ShouldContain("Address");
        paths.ShouldContain("Orders");
    }

    [TestMethod]
    public void Composite_MergesAndDedupesPaths()
    {
        var a = new IncludePathDefinition().Include("Customer", "Customer.Address");
        var b = new IncludePathDefinition().Include("Lines", "Customer.Address");

        var composite = new CompositeIncludePathDefinition(a, b);

        composite.GetPaths().OrderBy(p => p).ShouldBe(
            new[] { "Customer", "Customer.Address", "Lines" });
    }

    [TestMethod]
    public void Prefixed_PrependsAllPaths()
    {
        var inner = new IncludePathDefinition().Include("Address", "Orders");
        var prefixed = new PrefixedIncludePathDefinition("Parent", inner);

        prefixed.GetPaths().OrderBy(p => p).ShouldBe(new[] { "Parent.Address", "Parent.Orders" });
    }

    [TestMethod]
    public void Without_ExactMatchRemovesPath()
    {
        var def = new IncludePathDefinition().Include("Address", "Orders", "Orders.Lines");
        var filtered = def.Without("Address");

        filtered.GetPaths().ShouldNotContain("Address");
        filtered.GetPaths().ShouldContain("Orders");
        filtered.GetPaths().ShouldContain("Orders.Lines");
    }

    [TestMethod]
    public void WithoutPrefix_RemovesSubtree()
    {
        var def = new IncludePathDefinition().Include("Address", "Orders", "Orders.Lines", "Orders.Lines.Product");
        var filtered = def.WithoutPrefix("Orders");

        filtered.GetPaths().ShouldBe(new[] { "Address" });
    }

    [TestMethod]
    public void Without_ByExpression_StripsThatNavigation()
    {
        var def = new IncludePathDefinition().Include("Address", "Orders", "Orders.Lines");
        var filtered = def.Without<Customer>(c => c.Orders);

        filtered.GetPaths().ShouldBe(new[] { "Address" });
    }

    [TestMethod]
    public void Except_RemovesIntersection()
    {
        var def = new IncludePathDefinition().Include("Address", "Orders", "Orders.Lines");
        var remove = new IncludePathDefinition().Include("Orders");

        def.Except(remove).GetPaths().OrderBy(p => p).ShouldBe(new[] { "Address", "Orders.Lines" });
    }

    [TestMethod]
    public void WithoutRegex_DropsMatches()
    {
        var def = new IncludePathDefinition().Include("Address", "Orders", "Orders.Lines.Product");
        var filtered = def.WithoutRegex("^Orders\\.Lines\\..*$");

        filtered.GetPaths().OrderBy(p => p).ShouldBe(new[] { "Address", "Orders" });
    }

    [TestMethod]
    public void Without_GlobStarMatchesAcrossSegments()
    {
        var def = new IncludePathDefinition().Include("Orders", "Orders.Lines", "Orders.Lines.Product");
        var filtered = def.Without("Orders.**");

        filtered.GetPaths().ShouldBe(new[] { "Orders" });
    }

    [TestMethod]
    public void DistinctPaths_DedupesIdenticalEntries()
    {
        var def = new IncludePathDefinition().Include("Address", "Address", "Orders");
        IncludeDetailsExtensions.DistinctPaths(def).OrderBy(p => p).ShouldBe(new[] { "Address", "Orders" });
    }

    [TestMethod]
    public void Include_FromAnotherDefinition_MergesPaths()
    {
        var a = new IncludePathDefinition().Include("Customer", "Customer.Address");
        var b = new IncludeDefinitionFor<Order>().Include(a);

        b.GetPaths().OrderBy(p => p).ShouldBe(new[] { "Customer", "Customer.Address" });
    }

    [TestMethod]
    public void AttributeIncludePathDefinition_PicksUpDecoratedProperties()
    {
        var def = new AttributeIncludePathDefinition<Customer>(maxDepth: 3);

        var paths = def.GetPaths().ToArray();
        paths.ShouldContain("Address");
        paths.ShouldContain("Orders");
        paths.ShouldContain("Orders.Customer");
        paths.ShouldContain("Orders.Lines");
        paths.ShouldContain("Orders.Lines.Product");
    }
}
