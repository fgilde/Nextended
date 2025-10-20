using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.Rest.Azure.OData;
using Nextended.Core.OData;
using Nextended.Core.Tests.OData.Helpers;
using Nextended.Core.Tests.OData.Models;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Nextended.Core.Tests.OData;

public class FilterTest(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private readonly IQueryable<ODataTestModel> _models = new List<ODataTestModel>
    {
        new (){ IntField = 1, StringField = "Alpha", DecimalField = 10.5m, BoolField = true, DateField = new DateTime(2024, 1, 1), TestEnum = ODataTestEnum.Value1 },
        new (){ IntField = 2, StringField = "Beta", DecimalField = 20.0m, BoolField = false, DateField = new DateTime(2024, 2, 1), TestEnum = ODataTestEnum.Value2 },
        new (){ IntField = 3, StringField = "Gamma", DecimalField = 15.0m, BoolField = true, DateField = new DateTime(2023, 12, 1), TestEnum = ODataTestEnum.Value3 },
        new (){ IntField = 4, StringField = "Delta", DecimalField = 5.5m, BoolField = false, DateField = new DateTime(2023, 10, 1), TestEnum = ODataTestEnum.Value4 },
        new (){ IntField = 5, StringField = "Epsilon", DecimalField = 30.0m, BoolField = true, DateField = new DateTime(2024, 3, 1), TestEnum = ODataTestEnum.Value5 },
    }.AsQueryable();

    [Fact]
    public void Test_OData_Filter()
    {
        var queryModel = _models.Where(s => s.DecimalField > 10).Select(s => s.DecimalField).OrderBy(s => s).Skip(2).Take(1).ToODataModel();

        var query = "?" + queryModel.SelectString + "&" + queryModel.SkipString + "&" +
                    queryModel.TakeString + "&" + queryModel.FilterString;
        _output.WriteLine(query);
        // Arrange
        var options = TestHelpers.GetODataQueryOptions<ODataTestModel>(query);

        // Act
        var results = options.ApplyTo(_models).OfType<ISelectExpandWrapper>().ToList();
        foreach (var result in results)
        {
            _output.WriteLine(JsonSerializer.Serialize(result.ToDictionary()));
        }

    }

    [Fact]
    public void Test_DataFilter()
    {
        string filter = "?$filter=DateField gt datetimeoffset'2024-01-02T10:00:00'";
        var model = ODataQueryModel.Parse(filter);
        var result = model.ToQueryable(_models).ToList();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Test_FS()
    {
        Expression<Func<Language, bool>> filter = lang => (bool)lang.Active && lang.Name.StartsWith("A");
        var str = filter.ToFilterString();
        var leg = FilterString.Generate(filter, true);
        str.ShouldBeEquivalentTo(leg);
    }

}

class Language
{
    public bool Active { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
}