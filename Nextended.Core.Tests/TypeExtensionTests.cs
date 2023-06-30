using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;

namespace Nextended.Core.Tests;

[TestClass]
public class TypeExtensionsTest
{


    [TestMethod]
    public void TestIsCollection()
    {
        Assert.IsTrue(typeof(List<string>).IsCollection());
        Assert.IsFalse(typeof(string).IsCollection());
    }

    [TestMethod]
    public void TestIsNullableEnum()
    {
        Assert.IsTrue(typeof(DayOfWeek?).IsNullableEnum());
        Assert.IsFalse(typeof(DayOfWeek).IsNullableEnum());
    }

    [TestMethod]
    public void TestIsExpression()
    {
        Assert.IsTrue(typeof(Expression<Func<string, bool>>).IsExpression());
        Assert.IsFalse(typeof(string).IsExpression());
    }

    [TestMethod]
    public void TestIsString()
    {
        Assert.IsTrue(typeof(string).IsString());
        Assert.IsFalse(typeof(int).IsString());
    }

    [TestMethod]
    public void TestIsDecimal()
    {
        Assert.IsTrue(typeof(decimal).IsDecimal());
        Assert.IsFalse(typeof(int).IsDecimal());
    }


    [TestMethod]
    public void TestIsNullableAction()
    {
        Assert.IsFalse(typeof(Action).IsNullableAction());
        //Assert.IsTrue(typeof(Action?).IsNullableAction());
        Assert.IsFalse(typeof(Func<int, string>).IsNullableAction());
       // Assert.IsFalse(typeof(Func<int, string>?).IsNullableAction());
        Assert.IsFalse(typeof(int).IsNullableAction());
        Assert.IsFalse(typeof(int?).IsNullableAction());
    }

    [TestMethod]
    public void TestIsNullable()
    {
        Assert.IsTrue(typeof(int?).IsNullable());
        Assert.IsFalse(typeof(int).IsNullable());
        Assert.IsTrue(typeof(int?).IsNullable());
        Assert.IsTrue(typeof(Func<int, string>).IsNullable());
        Assert.IsTrue(typeof(Action).IsNullable());
    }

    [TestMethod]
    public void TestIsNullableOf()
    {
        Assert.IsTrue(typeof(int?).IsNullableOf<int>());
        Assert.IsFalse(typeof(int).IsNullableOf<int>());
        Assert.IsFalse(typeof(string).IsNullableOf<int>());
        //Assert.IsFalse(typeof(Func<int, string>?).IsNullableOf<int>());
        //Assert.IsFalse(typeof(Action?).IsNullableOf<int>());
    }

    [TestMethod]
    public void TestIsFunc()
    {
        Assert.IsTrue(typeof(Func<int, string>).IsFunc());
        //Assert.IsFalse(typeof(Func<int, string>?).IsFunc());
        Assert.IsFalse(typeof(Action).IsFunc());
        //Assert.IsFalse(typeof(Action?).IsFunc());
        Assert.IsFalse(typeof(int).IsFunc());
        Assert.IsFalse(typeof(int?).IsFunc());
    }

    [TestMethod]
    public void TestIsAction()
    {
        Assert.IsTrue(typeof(Action<>).IsAction());
        Assert.IsTrue(typeof(Action).IsAction());
//        Assert.IsFalse(typeof(Action?).IsAction());
       // Assert.IsFalse(typeof(Func<int, string>).IsAction());
       // Assert.IsFalse(typeof(Func<int, string>?).IsAction());
        Assert.IsFalse(typeof(int).IsAction());
        Assert.IsFalse(typeof(int?).IsAction());
    }

}