using System.Collections.Generic;
using System.Linq;
using Nextended.Core.OData;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Nextended.Core.Tests.OData;

public class UnitTest1
{
    private readonly ITestOutputHelper _outputHelper;

    public UnitTest1(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public class Test
    {
        public Test()
        {}

        public Test(int searchInt)
        {
            SearchInt = searchInt;
        }

        public Test(string searchStr)
        {
            SearchString = searchStr;
        }        
        
        public Test(int searchInt, string searchStr)
        {
            SearchInt = searchInt;
            SearchString = searchStr;
        }
        
        

        public string SearchString { get; } = string.Empty;
        public int SearchInt { get; } = 0;
    }
    
    [Fact]
    public void TestTestTest()
    {
        List<string> s1 = ["Entry1", "Entry2", "Entry3"];

        IQueryable<Test> t = new List<Test>().AsQueryable();

        var t1 = t.Select(s =>
        new {
             s.SearchInt,
             s.SearchString
        });

        _outputHelper.WriteLine(t1.ToSelectString());
    }
    [Fact]
    public void TestTest()
    {
        List<string> s1 = ["Entry1", "Entry2", "Entry3"];

        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(s => s.SearchInt == 0 && (s.SearchInt > 0 || s.SearchInt < 0)).Where(t1 => s1.Contains(t1.SearchString)).Where(s => s.SearchInt == 0);

        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void TestTest2()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(s => s.SearchString.StartsWith('s'));

        t.ToFilterString().ShouldBeEquivalentTo("startswith(SearchString, 's')");

        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void EndsWithTest()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(s => s.SearchString.EndsWith('s'));

        t.ToFilterString().ShouldBeEquivalentTo("endswith(SearchString, 's')");

        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void SearchInTest()
    {
        List<string> s = ["Entry1", "Entry2", "Entry3"];

        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(t1 => s.Contains(t1.SearchString));

        t.ToFilterString().ShouldBeEquivalentTo("search.in(SearchString,'Entry1,Entry2,Entry3')");
        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void WhereEqualTest()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(t1 => t1.SearchString == "Entry1");

        t.ToFilterString().ShouldBeEquivalentTo("SearchString eq 'Entry1'");
        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void WhereGreaterThanTest()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(t1 => t1.SearchInt > 0);

        t.ToFilterString().ShouldBeEquivalentTo("SearchInt gt 0");
        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void WhereGreaterThanEqualToTest()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(t1 => t1.SearchInt >= 0);

        t.ToFilterString().ShouldBeEquivalentTo("SearchInt gte 0");
        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void WhereLessThanTest()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(t1 => t1.SearchInt < 0);

        t.ToFilterString().ShouldBeEquivalentTo("SearchInt lt 0");
        _outputHelper.WriteLine(t.ToFilterString());
    }

    [Fact]
    public void WhereLessThanEqualToTest()
    {
        IQueryable<Test> t = new List<Test>().AsQueryable();

        t = t.Where(t1 => t1.SearchInt <= 0);

        t.ToFilterString().ShouldBeEquivalentTo("SearchInt lte 0");
        _outputHelper.WriteLine(t.ToFilterString());
    }
}
