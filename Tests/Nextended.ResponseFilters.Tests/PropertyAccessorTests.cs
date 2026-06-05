using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class PropertyAccessorTests
{
    [TestMethod]
    public void GetValue_ReturnsCurrentValue()
    {
        var order = new OrderDto { TotalCost = 42m };
        var prop = typeof(OrderDto).GetProperty(nameof(OrderDto.TotalCost))!;
        var accessor = PropertyAccessor.For(prop);

        accessor.GetValue(order).ShouldBe(42m);
    }

    [TestMethod]
    public void SetValue_AssignsNullForNullableValueType()
    {
        var order = new OrderDto { TotalCost = 42m };
        var accessor = PropertyAccessor.For(typeof(OrderDto).GetProperty(nameof(OrderDto.TotalCost))!);

        accessor.SetValue(order, null);

        order.TotalCost.ShouldBeNull();
    }

    [TestMethod]
    public void SetValue_AssignsValueForNonNullableValueType()
    {
        var order = new OrderDto { IsActive = true };
        var accessor = PropertyAccessor.For(typeof(OrderDto).GetProperty(nameof(OrderDto.IsActive))!);

        accessor.SetValue(order, false);

        order.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void For_ReturnsSameInstance_FromCache()
    {
        var prop = typeof(OrderDto).GetProperty(nameof(OrderDto.Email))!;
        PropertyAccessor.For(prop).ShouldBeSameAs(PropertyAccessor.For(prop));
    }

    [TestMethod]
    public void IndexerProperty_HasNoGetterOrSetter()
    {
        var indexer = typeof(IndexerDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        // Find the indexer (it appears under the name "Item")
        var itemProp = System.Array.Find(indexer, p => p.GetIndexParameters().Length > 0);
        itemProp.ShouldNotBeNull();

        var accessor = PropertyAccessor.For(itemProp);
        accessor.Getter.ShouldBeNull();
        accessor.Setter.ShouldBeNull();
    }
}
