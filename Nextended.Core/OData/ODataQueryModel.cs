using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using StringToExpression.LanguageDefinitions;

namespace Nextended.Core.OData;

#if NET8_0 || NET9_0
public class ODataQueryModel : IEquatable<ODataQueryModel>, IParsable<ODataQueryModel>
#else
    public class ODataQueryModel : IEquatable<ODataQueryModel>
#endif

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

    public bool IsValid => !string.IsNullOrWhiteSpace(FullString); // TODO: More advanced validation?

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


    public static ODataQueryModel For<T>(Func<IQueryable<T>, IQueryable> action)
    {
        IQueryable queryable = action(Enumerable.Empty<T>().AsQueryable());
        return queryable.ToODataModel();
    }

    public static bool TryParse(string s, out ODataQueryModel res) => TryParse(s, null, out res);

    public static bool TryParse(string s, IFormatProvider? culture, out ODataQueryModel res)
    {
        try
        {
            res = Parse(s, culture);
            return string.IsNullOrWhiteSpace(s) ? res != null : res?.IsValid == true;
        }
        catch (Exception)
        {
            res = null;
            return false;
        }
    }

    public static ODataQueryModel Parse(string s, IFormatProvider? culture = null)
    {
        return FromString(s);
    }

    public static ODataQueryModel FromString(string query)
    {
        var comparison = StringComparison.InvariantCultureIgnoreCase;
        if (string.IsNullOrEmpty(query))
            return new ODataQueryModel();
        var model = new ODataQueryModel();
        var parts = query.TrimStart('?').Split('&');
        foreach (var part in parts)
        {
            if (part.StartsWith("$filter=", comparison))
                model.Filter = part.Substring("$filter=".Length);
            else if (part.StartsWith("$select=", comparison))
                model.Select = part.Substring("$select=".Length);
            else if (part.StartsWith("$orderby=", comparison))
                model.OrderBy = part.Substring("$orderby=".Length);
            else if (part.StartsWith("$top=", comparison))
                model.Take = part.Substring("$top=".Length);
            else if (part.StartsWith("$skip=", comparison))
                model.Skip = part.Substring("$skip=".Length);
        }
        return model;
    }

}