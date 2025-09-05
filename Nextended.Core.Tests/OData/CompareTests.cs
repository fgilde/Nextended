using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Rest.Azure.OData;
using Nextended.Core.OData;
using Shouldly;
using Xunit;

namespace Nextended.Core.Tests.OData;

public class CompareTests
{
    [Fact]
    public void SelectTest()
    {
        List<string> s1 = ["Entry1", "Entry2", "Entry3"];

        IQueryable<UnitTest1.Test> t = new List<UnitTest1.Test>().AsQueryable();

        var t1 = t.Select(s =>
            new {
                s.SearchInt,
                s.SearchString
            });

        var selectString = t1.ToSelectString();
    }

    [Fact]
    public void CompareFilterTest()
    {
        IQueryable<UnitTest1.Test> t = new List<UnitTest1.Test>().AsQueryable();

        t = t.Where(s => s.SearchString.StartsWith('s'));

        var str = FilterString.Generate<UnitTest1.Test>(s => s.SearchString.StartsWith('s'), true);

        var filterString = t.ToFilterString();
        filterString.ShouldBeEquivalentTo("startswith(SearchString, 's')");

        filterString.Replace(" ", "").ShouldBeEquivalentTo(str);

        string fromExpression = ((Expression<Func<UnitTest1.Test, bool>>)(s => s.SearchString.StartsWith('s'))).ToFilterString();
        fromExpression.ShouldBeEquivalentTo(filterString);
    }

    [Fact]
    public void ModelParseTest()
    {
        var tests = new List<UnitTest1.Test>([new UnitTest1.Test(9, "Some"), new UnitTest1.Test(1, "Hello"), new UnitTest1.Test(5, "Sample"), new UnitTest1.Test(8, "Whatever")]);
        IQueryable<UnitTest1.Test> t = tests.AsQueryable();

        var x = t.Where(s => s.SearchString.StartsWith('S'))
            .OrderBy(test => test.SearchInt)
            .Select(test => test.SearchInt);

        ODataQueryModel model = x.ToODataModel();
        var newModel = ODataQueryModel.FromString(model.FullString);
        newModel.Equals(model).ShouldBe(true);
    }

    [Fact]
    public void ModelTest()
    {
        var tests = new List<UnitTest1.Test>([new UnitTest1.Test(9, "Some"), new UnitTest1.Test(1, "Hello"), new UnitTest1.Test(5, "Sample"), new UnitTest1.Test(8, "Whatever")]);
        IQueryable<UnitTest1.Test> t = tests.AsQueryable();

        IQueryable<int> x = t.Where(s => s.SearchString.StartsWith('S'))
            .OrderBy(test => test.SearchInt)
            .Select(test => test.SearchInt);

        ODataQueryModel model = x.ToODataModel();
        model.ShouldNotBeNull();


        var qres = model.ToQueryableWithSelect(t);
        var qres2 = model.ToQueryable<UnitTest1.Test, int>(t);
        
        
        List<UnitTest1.Test> xs = model.ToQueryable(t).ToList();
        
        var exp = model.ToExpression<UnitTest1.Test>();
        var res = tests.AsQueryable().Where(exp).ToList();
        
    }

    [Fact]
    public void ModelTest2()
    {
        var tests = new List<UnitTest1.Test>([new UnitTest1.Test(9, "Some"), new UnitTest1.Test(1, "Hello"), new UnitTest1.Test(5, "Sample"), new UnitTest1.Test(8, "Whatever")]);
        IQueryable<UnitTest1.Test> t = tests.AsQueryable();


        var model = ODataQueryModel.For<UnitTest1.Test>(q => q.Where(s => s.SearchString.StartsWith('S'))
            .OrderBy(test => test.SearchInt)
            .Select(test => test.SearchInt));
        
        model.ShouldNotBeNull();

        var qres = model.ToQueryableWithSelect(t);
        var qres2 = model.ToQueryable<UnitTest1.Test, int>(t);


        List<UnitTest1.Test> xs = model.ToQueryable(t).ToList();

        var exp = model.ToExpression<UnitTest1.Test>();
        var res = tests.AsQueryable().Where(exp).ToList();

    }
}