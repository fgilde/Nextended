using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using Nextended.Core.Tests.classes;
using System.Linq;
using Nextended.Core.Helper;
using System.Reflection;

namespace Nextended.Core.Tests;

[TestClass]
public class OtherTests
{
    [TestMethod]
    public void StringEnsureTest()
    {
        var s1 = "Hallo World!".EnsureEndsWith("!").EnsureStartsWith("H");
        var s2 = "allo World".EnsureEndsWith("!").EnsureStartsWith("H");
        Assert.AreEqual(s1, s2);
    }

    [TestMethod]
    public void AllOfTest()
    {
        

        var p = new ProductDto()
        {
            Barcode = "123",
            Name = "Peter",
            UploadRequest = new UploadRequest()
        };

        var ul = p.AllOf<UploadRequest>();
        Assert.IsNotNull(ul);
        Assert.IsTrue(ul.Length == 1);

        var strings = p.AllOf<string>();
        Assert.IsNotNull(strings);
        Assert.IsTrue(strings.Length > 1);

        var barcode = strings.FirstOrDefault(s => s == "123");
        Assert.IsTrue(!string.IsNullOrEmpty(barcode));
    }

    [TestMethod]
    public void AllOfPrivateTest()
    {
        var p = new ProductDto()
        {
            Barcode = "123",
            Name = "Peter",
            UploadRequest = new UploadRequest()
        };
        
        var guids = p.AllOf<Guid>();
        Assert.IsTrue(guids.Length == 1);  // Public
        guids = p.AllOf<Guid>(ReflectReadSettings.All);
        Assert.IsTrue(guids.Length == 2); // Public and private from ProductDto

        guids = p.AllOf<Guid>(ReflectReadSettings.AllWithHierarchyTraversal);
        Assert.IsTrue(guids.Length == 3); // Public, private from ProductDto and private from base

        var floats = p.AllOf<double>();
        Assert.IsTrue(floats.Length == 1); // Public field

        floats = p.AllOf<double>(ReflectReadSettings.AllWithHierarchyTraversal);
        Assert.IsTrue(floats.Length == 3); // Public field and 2 backing fields
    }


}