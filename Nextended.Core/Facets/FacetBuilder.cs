#if !NETSTANDARD

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Attributes;
using Nextended.Core.Extensions;

namespace Nextended.Core.Facets;

/// <summary>
/// Builds facet definitions (groups/options/ranges) for a given IQueryable that already has OData applied.
/// - Supports scalar and navigation properties via ValuePath/LabelPath
/// - Uses Dynamic LINQ (dot-paths) for grouping/counts
/// - Emits OData filter snippets (slash-paths) and correct literals depending on FacetBuilderOptions.BuildLiterals
/// - Supports disjunctive faceting (OR within a group) controlled by options and GroupOperator
/// </summary>
[RegisterAs(typeof(IFacetBuilder), RegisterAsImplementation = true)]
public sealed class FacetBuilder(FacetBuilderOptions? options = null,
                                 IODataLiteralFormatter? literal = null
) : IFacetBuilder
{
    
    private FacetBuilderOptions _options = options ?? new FacetBuilderOptions();
    private readonly IODataLiteralFormatter _literal = literal ?? new DefaultODataLiteralFormatter();
    private Func<string, Type?, string> _localizerFn;


    public IFacetBuilder WithLocalizationFunc(Func<string, Type?, string> localizerFn) => this.SetProperties(b => b._localizerFn = localizerFn);
    public IFacetBuilder WithOptions(FacetBuilderOptions options) => this.SetProperties(b => b._options = options);

    public async Task<List<FacetGroup>> BuildAsync<T>(IQueryable<T> alreadyFilteredQuery, CancellationToken ct = default)
        => await BuildInternalAsync(alreadyFilteredQuery, applied: [], conjunctiveOnly: true, ct);

    // Disjunctive-capable: provide baseQuery (no OData) + applied chips
    public async Task<List<FacetGroup>> BuildAsync<T>(IQueryable<T> baseQuery, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = default)
        => await BuildInternalAsync(baseQuery, applied, conjunctiveOnly: !_options.DisjunctiveFacets, ct);

    private async Task<List<FacetGroup>> BuildInternalAsync<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        bool conjunctiveOnly,
        CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                             .Select(p => (Prop: p, Facet: p.GetCustomAttributes<ProvideFacetAttribute>(true).FirstOrDefault()))
                             .Where(x => x.Facet != null)
                             .OrderBy(x => x.Facet!.Order);


        var metaByKey = props.Select((x) =>
        {
            (var propertyInfo, var ProvideFacetAttribute) = (x.Prop, x.Facet!);
            var scalarType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            var rawValuePath = string.IsNullOrWhiteSpace(ProvideFacetAttribute.ValuePath) ? propertyInfo.Name : ProvideFacetAttribute.ValuePath!;

            var valueClr = ProvideFacetAttribute.ValueType ?? (string.IsNullOrWhiteSpace(ProvideFacetAttribute.ValuePath) ? scalarType : typeof(string));

            return new FacetMeta
            {
                Key = propertyInfo.Name.ToCamel(),
                Field = propertyInfo.Name,
                ValuePathDyn = ToDynPath(rawValuePath),
                LabelPathDyn = string.IsNullOrWhiteSpace(ProvideFacetAttribute.LabelPath) ? ToDynPath(rawValuePath) : ToDynPath(ProvideFacetAttribute.LabelPath),
                ValueClr = valueClr,
                ProvideFacetAttribute = ProvideFacetAttribute,
                ScalarType = scalarType,
                FacetGroup = new FacetGroup
                {
                    Key = propertyInfo.Name.ToCamel(),
                    Label = L(ProvideFacetAttribute.Label ?? propertyInfo.Name),
                    Field = propertyInfo.Name,
                    ValuePath = ToODataPath(rawValuePath),
                    LabelPath = string.IsNullOrWhiteSpace(ProvideFacetAttribute.LabelPath) ? null : ToODataPath(ProvideFacetAttribute.LabelPath),
                    ValueClrType = valueClr.AssemblyQualifiedName!,
                    Type = ProvideFacetAttribute.Type,
                    GroupOperator = ProvideFacetAttribute.GroupOperator,
                    MultiSelect = ProvideFacetAttribute.MultiSelect,
                    Order = ProvideFacetAttribute.Order,
                    Options = new List<FacetOption>()
                }
            };
        }).ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        var query = metaByKey.Values.Where(m => m.ProvideFacetAttribute.Type is FacetType.CheckboxList or FacetType.TokenList or FacetType.Radio)
                             .Select(facetMeta => BuildFaceQuery(
                                                                 facetMeta,
                                                                 baseQuery,
                                                                 applied,
                                                                 metaByKey
                                                                )
                                    )
                             .Aggregate((q1, q2) => q1.Union(q2));

        // TODO ASYNC
        //var facetResults = _options.ComputeDynamicLists
        //                       ? await query.ToListAsync(ct)
        //                       : new();

        var facetResults = _options.ComputeDynamicLists
            ? query.ToList()
            : new();

        var groupedFacetResults = facetResults
            .GroupBy(fr => fr.Discriminator, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, StringComparer.OrdinalIgnoreCase);


        foreach (var facetMeta in metaByKey.Values)
        {
            var groupSw = System.Diagnostics.Stopwatch.StartNew();
            var group = facetMeta.FacetGroup;
            var ProvideFacetAttribute = facetMeta.ProvideFacetAttribute;
            if (ProvideFacetAttribute.Type is FacetType.CheckboxList or FacetType.TokenList or FacetType.Radio)
            {
                if (_options.ComputeDynamicLists)
                {
                    if (groupedFacetResults.TryGetValue(group.Key, out var rows))
                    {
                        foreach (var row in rows)
                        {
                            var vl = ToValueLiteral(row.Value, facetMeta.ValueClr);

                            group.Options.Add(
                                              new FacetOption
                                              {
                                                  Value = vl.Display,
                                                  Label = L(row.Label ?? vl.Display, facetMeta.ScalarType),
                                                  Count = Convert.ToInt64(row.Count),
                                                  Enabled = true,
                                                  OData = $"{group.ValuePath} eq {vl.Literal}"
                                              }
                                             );
                        }
                    }

                    IncludeSelectedEvenIfMissing(group, applied, facetMeta.ScalarType);
                }
            }
            else if (ProvideFacetAttribute.Type is FacetType.Range or FacetType.DateRange)
            {
                var dt = facetMeta.ValueClr;
                group.Range = DefaultDateOrNumberRange(group.ValuePath, dt);

                if (_options.ComputeDynamicLists && group.Range?.Presets.Any() == true)
                {
                    var dynPath = ToDynPath(group.ValuePath);
                    foreach (var preset in group.Range.Presets)
                    {
                        if (DateTimeOffset.TryParse(preset.Value.From, out var from) &&
                            DateTimeOffset.TryParse(preset.Value.To, out var to))
                        {
                            //var cnt = await ApplyAppliedFiltersExceptGroup(baseQuery, applied, group.Key, metaByKey)
                            //    .Where($"{dynPath} >= @0 && {dynPath} < @1", from, to)
                            //    .CountAsync(ct);

                            // TODO ASYNC
                            var cnt = ApplyAppliedFiltersExceptGroup(baseQuery, applied, group.Key, metaByKey)
                                .Where($"{dynPath} >= @0 && {dynPath} < @1", from, to)
                                .Count();

                            preset.Count = cnt;
                        }
                    }
                }
            }

            groupSw.Stop();
            group.BuildDuration = groupSw.Elapsed;

        }


        return metaByKey.Values.Select(m => m.FacetGroup).ToList();
    }

    private IQueryable<FacetResult> BuildFaceQuery<T>(
        FacetMeta meta,
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        Dictionary<string, FacetMeta> metaByKey
    )
    {

        // choose context query for this group
        FacetGroup groupDto = meta.FacetGroup;

        bool disjunctiveForGroup =
            _options.DisjunctiveFacets ||
            (_options.DisjunctiveByGroupOperator && groupDto.GroupOperator == FacetGroupOperator.Or);

        IQueryable<T> ctxQ = disjunctiveForGroup
                                 ? ApplyAppliedFiltersExceptGroup(baseQuery, applied, meta.Key, metaByKey)
                                 : baseQuery;

        var top = meta.ProvideFacetAttribute.TopDistinct > 0
                      ? meta.ProvideFacetAttribute.TopDistinct
                      : _options.DefaultTopDistinct;

        // ALWAYS exclude nulls to avoid null Key in grouping
        IQueryable query = ctxQ.Where($"{meta.ValuePathDyn} != null")
                                .GroupBy($"new ({meta.ValuePathDyn} as value, {meta.LabelPathDyn} as label)")
                               .OrderBy("Count() desc");

        if (top != null)
        {
            query = query.Take(top.Value);
        }

        return query
              .Select<FacetResult>(
                   $"""
                    new (
                        "{meta.Key}" as {nameof(FacetResult.Discriminator)},
                        As(Key.value, "string")  as {nameof(FacetResult.Value)},
                        As(Key.label, "string") as {nameof(FacetResult.Label)},
                        Count() as {nameof(FacetResult.Count)}
                        )
                    """
               );
    }

    private string L(string key, Type? type = null)
    {
        return _localizerFn != null ? _localizerFn(key, type) : key;
    }

    private string ToDynPath(string raw) => raw.Replace("/", ".");
    private string ToODataPath(string raw) => raw.Replace(".", "/");

    // ----- disjunctive helper -----

    private IQueryable<T> ApplyAppliedFiltersExceptGroup<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        string currentGroupKey,
        IReadOnlyDictionary<string, FacetMeta> metaByKey)
    {
        if (applied.Count == 0) return baseQuery;

        var otherGroups = applied
            .Where(a => !string.Equals(a.GroupKey, currentGroupKey, StringComparison.OrdinalIgnoreCase))
            .GroupBy(a => a.GroupKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var q = baseQuery;

        foreach (var grp in otherGroups)
        {
            if (!metaByKey.TryGetValue(grp.Key, out var meta))
                continue;

            var values = grp.SelectMany(g => g.Values)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
            if (values.Count == 0) continue;

            var pars = new List<object?>();
            var ors = new List<string>();

            foreach (var v in values)
            {
                pars.Add(ParseValue(v, meta.ValueClr));
                ors.Add($"{meta.ValuePathDyn} == @{pars.Count - 1}");
            }

            var predicate = "(" + string.Join(" OR ", ors) + ")";
            q = q.Where(predicate, pars.ToArray());
        }

        return q;
    }

    private static object? ParseValue(string s, Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t == typeof(string)) return s;
        if (t == typeof(Guid)) return Guid.Parse(s);
        if (t == typeof(bool)) return bool.Parse(s);
        if (t.IsEnum) return Enum.Parse(t, s, true);
        if (t == typeof(DateTimeOffset)) return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
        if (t == typeof(DateTime)) return DateTime.SpecifyKind(DateTime.Parse(s, CultureInfo.InvariantCulture), DateTimeKind.Utc);
        if (t == typeof(int)) return int.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(long)) return long.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(short)) return short.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(byte)) return byte.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(double)) return double.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(float)) return float.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(decimal)) return decimal.Parse(s, CultureInfo.InvariantCulture);
        return s;
    }

    private void IncludeSelectedEvenIfMissing(
        FacetGroup group,
        IReadOnlyList<AppliedFacet> applied,
        Type scalarType
    )
    {
        var chips = applied.Where(a => string.Equals(a.GroupKey, group.Key, StringComparison.OrdinalIgnoreCase));
        foreach (var chip in chips)
        {
            foreach (var v in chip.Values)
            {
                if (!group.Options.Any(o => string.Equals(o.Value, v, StringComparison.OrdinalIgnoreCase)))
                {
                    group.Options.Add(new FacetOption
                    {
                        Value = v,
                        Label = L(v, scalarType),
                        Count = 0,
                        Enabled = true
                    });
                }
            }
        }
    }

    // ----------------- literal & range helpers -----------------

    private ValueLiteral ToValueLiteral(object? rawValue, Type clrType)
    {
        var display = rawValue?.ToString() ?? "";

        object? coerced = rawValue;
        try
        {
            var t = Nullable.GetUnderlyingType(clrType) ?? clrType;
            if (rawValue is not null && rawValue.GetType() != t)
            {
                if (t == typeof(Guid)) coerced = Guid.Parse(display);
                else coerced = Convert.ChangeType(rawValue, t, CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            coerced = display;
        }

        var literal = _options.BuildLiterals
            ? _literal.ToLiteral(coerced, clrType)
            : DefaultODataLiteralFormatter.ToSimpleLiteral(coerced, clrType);

        return new ValueLiteral(display, literal);
    }

    private FacetRangeDefinition DefaultDateOrNumberRange(string odataPath, Type type)
    {
        var dt = Nullable.GetUnderlyingType(type) ?? type;
        var range = new FacetRangeDefinition
        {
            DataType = InferRangeType(dt),
            Selected = null,
            Presets = new List<FacetRangeBucket>()
        };

        if (dt == typeof(DateTime) || dt == typeof(DateTimeOffset))
        {
            var now = DateTimeOffset.UtcNow;

            string RangeClauseLocal(object fromInclusive, object toExclusive)
            {
                if (_options.BuildLiterals)
                    return _literal.RangeClause(odataPath, fromInclusive, toExclusive, dt);
                var from = DefaultODataLiteralFormatter.ToSimpleLiteral(fromInclusive, dt);
                var to = DefaultODataLiteralFormatter.ToSimpleLiteral(toExclusive, dt);
                return $"({odataPath} ge {from} and {odataPath} lt {to})";
            }

            FacetRangeBucket buildRangeValueForDays(int days) => new()
            {
                Key = $"last{days}d",
                Label = string.Format(L("Last {0} days"), days),
                Value = new FacetRangeValue
                {
                    From = FromDays(-days).ToString("yyyy-MM-dd"),
                    To = NextDayUtcMidnight().ToString("yyyy-MM-dd"),
                    OData = RangeClauseLocal(FromDays(-7), NextDayUtcMidnight())
                }
            };

            DateTimeOffset FromDays(int days) => now.AddDays(days);
            DateTimeOffset NextDayUtcMidnight() => new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);

            range.Presets = new List<FacetRangeBucket>
            {
                buildRangeValueForDays(7),
                buildRangeValueForDays(14),
                buildRangeValueForDays(30),
            };
        }

        return range;
    }


    private static FacetRangeDataType InferRangeType(Type t)
    {
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return FacetRangeDataType.DateTime;
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return FacetRangeDataType.Decimal;
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return FacetRangeDataType.Number;
        return FacetRangeDataType.Number;
    }


    private sealed class ValueLiteral
    {
        public string Display { get; }
        public string Literal { get; }
        public ValueLiteral(string display, string literal) { Display = display; Literal = literal; }
    }

    private sealed class FacetMeta
    {
        public required string Key { get; init; }
        public required string Field { get; init; }
        public required string ValuePathDyn { get; init; }
        public required Type ValueClr { get; init; } = typeof(string);

        public required FacetGroup FacetGroup { get; init; }
        public required ProvideFacetAttribute ProvideFacetAttribute { get; init; }
        public required string LabelPathDyn { get; init; }
        public required Type ScalarType { get; init; }
    }

    private sealed class FacetResult
    {
        public string Discriminator { get; set; }
        public string Value { get; set; }
        public string? Label { get; set; }
        public int Count { get; set; }

    }

}
#endif