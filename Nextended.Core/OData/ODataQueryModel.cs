using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using StringToExpression.LanguageDefinitions;

namespace Nextended.Core.OData;

public class ODataQueryModel: IEquatable<ODataQueryModel>
{

    public string Filter { get; set; }
    public string FilterString => string.IsNullOrEmpty(Filter) ? "" : "$filter=" + Filter;
    
    public string Select { get; set; }
    public string SelectString => string.IsNullOrEmpty(Select) ? "" : "$select=" + Select;
    
    public string OrderBy { get; set; }
    public string OrderByString => string.IsNullOrEmpty(OrderBy) ? "" : "$orderby=" + OrderBy;
    
    public string Take { get; set; }
    public string TakeString => string.IsNullOrEmpty(Take) ? "" : "$top=" + Take;

    public string Skip { get; set; }
    public string SkipString => string.IsNullOrEmpty(Skip) ? "" : "$skip=" + Skip;
    
    public string FullString =>
        string.Join("&",
            new[] { FilterString, SelectString, OrderByString, TakeString, SkipString }.Where(x =>
                !string.IsNullOrEmpty(x)));
    
    
    public static ODataQueryModel FromString(string query)
    {
        if (string.IsNullOrEmpty(query))
            return new ODataQueryModel();
        var model = new ODataQueryModel();
        var parts = query.TrimStart('?').Split('&');
        foreach (var part in parts)
        {
            if (part.StartsWith("$filter="))
                model.Filter = part.Substring("$filter=".Length);
            else if (part.StartsWith("$select="))
                model.Select = part.Substring("$select=".Length);
            else if (part.StartsWith("$orderby="))
                model.OrderBy = part.Substring("$orderby=".Length);
            else if (part.StartsWith("$top="))
                model.Take = part.Substring("$top=".Length);
            else if (part.StartsWith("$skip="))
                model.Skip = part.Substring("$skip=".Length);
        }
        return model;
    }


    public Expression<Func<T, bool>> ToExpression<T>() => !string.IsNullOrEmpty(Filter) && Filter != "{}" ? new ODataFilterLanguage().Parse<T>(Filter) : null;

    public IQueryable<TSource> ToQueryable<TSource>(IQueryable<TSource> source)
    {
        var query = source;

        if (!string.IsNullOrEmpty(Filter))
            query = query.Where(ToExpression<TSource>());

        if (!string.IsNullOrEmpty(OrderBy))
            query = query.OrderBy(OrderBy);

        if (!string.IsNullOrEmpty(Skip) && int.TryParse(Skip, out var skipVal))
            query = query.Skip(skipVal);

        if (!string.IsNullOrEmpty(Take) && int.TryParse(Take, out var takeVal))
            query = query.Take(takeVal);


        return query;
    }

    public IQueryable<TResult> ToQueryable<TSource, TResult>(IQueryable<TSource> source)
    {
        var res = ToQueryableWithSelect(source);
        return (IQueryable<TResult>)res;
    }

    public IQueryable ToQueryableWithSelect<TSource>(IQueryable<TSource> source)
    {
        IQueryable result = ToQueryable(source);
        if (!string.IsNullOrEmpty(Select))
            result = result.Select($"{Select}");

        return result;
    }

    #region Equality Members

    public bool Equals(ODataQueryModel other) => FullString == other.FullString;


    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ODataQueryModel)obj);
    }

    public override int GetHashCode() => FullString.GetHashCode();

    public static bool operator ==(ODataQueryModel obj1, ODataQueryModel obj2)
    {
        if (ReferenceEquals(obj1, obj2))
            return true;
        if (ReferenceEquals(obj1, null))
            return false;
        if (ReferenceEquals(obj2, null))
            return false;
        return obj1.Equals(obj2);
    }
    public static bool operator !=(ODataQueryModel obj1, ODataQueryModel obj2) => !(obj1 == obj2);

    #endregion

}