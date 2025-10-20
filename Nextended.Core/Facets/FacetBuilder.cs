#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hub.Attributes.Facattes;
using Nextended.Core.Attributes;

namespace Nextended.Core.Facets;

/// <summary>
/// Builds facet definitions (groups/options/ranges) for a given IQueryable that already has OData applied.
/// - Supports scalar and navigation properties via ValuePath/LabelPath
/// - Uses Dynamic LINQ (dot-paths) for grouping/counts
/// - Emits OData filter snippets (slash-paths) and correct literals depending on FacetBuilderOptions.BuildLiterals
/// - Supports disjunctive faceting (OR within a group) controlled by options and GroupOperator
/// </summary>
[RegisterAs(typeof(IFacetBuilder))]
public sealed class FacetBuilder : IFacetBuilder
{
    private FacetBuilderOptions _options;
    private readonly IODataLiteralFormatter _literal;
    private Func<string, string>? _localizerFn;

    public FacetBuilder(FacetBuilderOptions? options = null, IODataLiteralFormatter? literal = null)
    {
        _options = options ?? new FacetBuilderOptions();
        _literal = literal ?? new DefaultODataLiteralFormatter();
    }

    public IFacetBuilder WithOptions(FacetBuilderOptions options)
    {
        _options = options;
        return this;
    }

    public IFacetBuilder WithLocalizationFunc(Func<string, string> localizerFn)
    {
        _localizerFn = localizerFn;
        return this;
    }

    // Conjunctive: compute counts on already-filtered query
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
        var type = typeof(T);

        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Select(p => new { Prop = p, Facet = p.GetCustomAttributes(typeof(ProvideFacetAttribute), true).Cast<ProvideFacetAttribute>().FirstOrDefault() })
                        .Where(x => x.Facet != null)
                        .OrderBy(x => x.Facet!.Order)
                        .ToList();

        var groups = new List<FacetGroup>(props.Count);

        // Meta map for disjunctive mode
        var metaByKey = props.Select(x =>
        {
            var p = x.Prop;
            var f = x.Facet!;
            var scalarType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

            var rawValuePath = string.IsNullOrWhiteSpace(f.ValuePath) ? p.Name : f.ValuePath!;
            var valuePathOData = ToODataPath(rawValuePath);
            var valuePathDyn = ToDynPath(rawValuePath);

            var valueClr = f.ValueType ?? (string.IsNullOrWhiteSpace(f.ValuePath) ? scalarType : typeof(string));

            return new FacetMeta
            {
                Key = ToCamel(p.Name),
                Field = p.Name,
                ValuePathOData = valuePathOData,
                ValuePathDyn = valuePathDyn,
                ValueClr = valueClr
            };
        }).ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var x in props)
        {
            ct.ThrowIfCancellationRequested();

            var p = x.Prop;
            var f = x.Facet!;
            var scalarType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

            var rawValuePath = string.IsNullOrWhiteSpace(f.ValuePath) ? p.Name : f.ValuePath!;
            var rawLabelPath = f.LabelPath;

            var valuePathDyn = ToDynPath(rawValuePath);    // e.g. TransportMode.Id
            var valuePathOData = ToODataPath(rawValuePath);  // e.g. TransportMode/Id
            var labelPathDyn = string.IsNullOrWhiteSpace(rawLabelPath) ? null : ToDynPath(rawLabelPath!);

            var valueClr = f.ValueType ?? (string.IsNullOrWhiteSpace(f.ValuePath) ? scalarType : typeof(string));

            var group = new FacetGroup
            {
                Key = ToCamel(p.Name),
                Label = L(f.Label),
                Field = p.Name,
                ValuePath = valuePathOData,
                LabelPath = string.IsNullOrWhiteSpace(rawLabelPath) ? null : ToODataPath(rawLabelPath!),
                ValueClrType = valueClr.AssemblyQualifiedName,
                Type = f.Type,
                GroupOperator = f.GroupOperator,
                MultiSelect = f.MultiSelect,
                Order = f.Order,
                Options = new List<FacetOption>()
            };

            // choose context query for this group
            bool disjunctiveForGroup =
                (_options.DisjunctiveFacets) ||
                (_options.DisjunctiveByGroupOperator && group.GroupOperator == FacetGroupOperator.Or);

            IQueryable<T> ctxQ = disjunctiveForGroup
                ? ApplyAppliedFiltersExceptGroup(baseQuery, applied, group.Key, metaByKey)
                : baseQuery;

            if (f.Type is FacetType.CheckboxList or FacetType.TokenList or FacetType.Radio)
            {
                if (_options.ComputeCounts)
                {
                    var top = f.TopDistinct > 0 ? f.TopDistinct : _options.DefaultTopDistinct;

                    // ALWAYS exclude nulls to avoid null Key in grouping
                    var qq = ctxQ.Where($"{valuePathDyn} != null");

                    if (!string.IsNullOrEmpty(labelPathDyn))
                    {
                        IQueryable queryable = qq.GroupBy($"new ({valuePathDyn} as value, {labelPathDyn} as label)")
                            .Select("new (Key.value as K, Key.label as L, Count() as C)")
                            .OrderBy("C desc");
                        if (top != null)
                            queryable = queryable.Take(top.Value);

                        List<dynamic> rows = await queryable
                            .ToDynamicListAsync(ct);

                        foreach (var r in rows)
                        {
                            var rawVal = r.K;
                            var lbl = r.L is null ? Convert.ToString(rawVal) ?? "" : Convert.ToString(r.L)!;
                            var vl = ToValueLiteral(rawVal, valueClr);

                            group.Options.Add(new FacetOption
                            {
                                Value = vl.Display,
                                Label = L(string.IsNullOrEmpty(lbl) ? vl.Display : lbl),
                                Count = Convert.ToInt64(r.C),
                                Enabled = true,
                                OData = $"{valuePathOData} eq {vl.Literal}"
                            });
                        }
                    }
                    else
                    {
                        IQueryable queryable = qq.GroupBy(valuePathDyn)
                            .Select("new (Key as K, Count() as C)")
                            .OrderBy("C desc");
                        if (top != null)
                            queryable = queryable.Take(top.Value);

                        var rows = await queryable
                            .ToDynamicListAsync(ct);

                        foreach (var r in rows)
                        {
                            var rawVal = r.K;
                            var vl = ToValueLiteral(rawVal, valueClr);

                            group.Options.Add(new FacetOption
                            {
                                Value = vl.Display,
                                Label = L(vl.Display),
                                Count = Convert.ToInt64(r.C),
                                Enabled = true,
                                OData = $"{valuePathOData} eq {vl.Literal}"
                            });
                        }
                    }

                    // Keep selected options visible even if they wouldn't appear (OR UX)
                    IncludeSelectedEvenIfMissing(group, applied);
                }
            }
            else if (f.Type is FacetType.Range or FacetType.DateRange)
            {
                var dt = f.ValueType ?? scalarType;
                group.Range = DefaultDateOrNumberRange(valuePathOData, dt);

                // Optional: counts for date presets (potentially expensive)
                if (_options.ComputeCounts && group.Range?.Presets?.Any() == true)
                {
                    var dynPath = ToDynPath(group.ValuePath ?? group.Field);
                    foreach (var preset in group.Range.Presets)
                    {
                        if (DateTimeOffset.TryParse(preset.Value.From, out var from) &&
                            DateTimeOffset.TryParse(preset.Value.To, out var to))
                        {
                            var cnt = ApplyAppliedFiltersExceptGroup(baseQuery, applied, group.Key, metaByKey)
                                .Where($"{dynPath} >= @0 && {dynPath} < @1", from, to)
                                .Count();
                            preset.Count = cnt;
                        }
                    }
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    // ----- disjunctive helper -----

    private IQueryable<T> ApplyAppliedFiltersExceptGroup<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        string currentGroupKey,
        IReadOnlyDictionary<string, FacetMeta> metaByKey)
    {
        if (applied == null || applied.Count == 0) return baseQuery;

        var otherGroups = applied
            .Where(a => !string.Equals(a.GroupKey, currentGroupKey, StringComparison.OrdinalIgnoreCase))
            .GroupBy(a => a.GroupKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var q = baseQuery;

        foreach (var grp in otherGroups)
        {
            if (!metaByKey.TryGetValue(grp.Key, out var meta))
                continue;

            var dynPath = meta.ValuePathDyn;
            var clrType = meta.ValueClr;

            var values = grp.SelectMany(g => g.Values ?? Enumerable.Empty<string>())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
            if (values.Count == 0) continue;

            var pars = new List<object?>();
            var ors = new List<string>();

            foreach (var v in values)
            {
                pars.Add(ParseValue(v, clrType));
                ors.Add($"{dynPath} == @{pars.Count - 1}");
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

    private void IncludeSelectedEvenIfMissing(FacetGroup group, IReadOnlyList<AppliedFacet> applied)
    {
        var chips = applied.Where(a => string.Equals(a.GroupKey, group.Key, StringComparison.OrdinalIgnoreCase));
        foreach (var chip in chips)
        {
            foreach (var v in chip.Values ?? Enumerable.Empty<string>())
            {
                if (!group.Options.Any(o => string.Equals(o.Value, v, StringComparison.OrdinalIgnoreCase)))
                {
                    group.Options.Add(new FacetOption
                    {
                        Value = v,
                        Label = L(v),
                        Count = 0,
                        Enabled = true
                    });
                }
            }
        }
    }

    // ----------------- literal & range helpers -----------------

    private string L(string key)
    {
        return _localizerFn != null ? _localizerFn(key) : key;
    }

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
            : ToSimpleLiteral(coerced, clrType);

        return new ValueLiteral(display, literal);
    }

    /// <summary>
    /// Simple literal builder without OData type prefixes (for BuildLiterals=false).
    /// - strings are single-quoted
    /// - booleans are true/false
    /// - DateTime/DateTimeOffset are ISO8601 and quoted
    /// - enums use their name as quoted string
    /// - numerics are culture-invariant without quotes
    /// </summary>
    public static string ToSimpleLiteral(object? value, Type type)
    {
        if (value is null) return "null";
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(string)) return $"'{value.ToString()!.Replace("'", "''")}'";
        if (t == typeof(bool)) return (bool)value ? "true" : "false";
        if (t == typeof(Guid)) return $"{value}";
        if (t == typeof(DateTimeOffset)) { var dto = value is DateTimeOffset d ? d : new DateTimeOffset(Convert.ToDateTime(value, CultureInfo.InvariantCulture), TimeSpan.Zero); return $"'{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}'"; }
        if (t == typeof(DateTime)) { var dt = value is DateTime d ? (d.Kind == DateTimeKind.Utc ? d : d.ToUniversalTime()) : Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToUniversalTime(); return $"'{dt:yyyy-MM-ddTHH:mm:ssZ}'"; }
        if (t.IsEnum) { var name = ResolveEnumNameSafe(t, value) ?? value.ToString()!; return $"'{name.Replace("'", "''")}'"; }
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
            t == typeof(double) || t == typeof(float) || t == typeof(decimal))
            return Convert.ToString(value, CultureInfo.InvariantCulture)!;

        return $"'{value.ToString()!.Replace("'", "''")}'";
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
                var from = ToSimpleLiteral(fromInclusive, dt);
                var to = ToSimpleLiteral(toExclusive, dt);
                return $"({odataPath} ge {from} and {odataPath} lt {to})";
            }

            DateTimeOffset FromDays(int days) => now.AddDays(days);
            DateTimeOffset NextDayUtcMidnight() => new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);

            range.Presets = new List<FacetRangeBucket>
            {
                new FacetRangeBucket{
                    Key="last7d", Label="Last 7 days",
                    Value = new FacetRangeValue{
                        From = FromDays(-7).ToString("yyyy-MM-dd"),
                        To   = NextDayUtcMidnight().ToString("yyyy-MM-dd"),
                        OData = RangeClauseLocal(FromDays(-7), NextDayUtcMidnight())
                    }
                },
                new FacetRangeBucket{
                    Key="last30d", Label="Last 30 days",
                    Value = new FacetRangeValue{
                        From = FromDays(-30).ToString("yyyy-MM-dd"),
                        To   = NextDayUtcMidnight().ToString("yyyy-MM-dd"),
                        OData = RangeClauseLocal(FromDays(-30), NextDayUtcMidnight())
                    }
                }
            };
        }

        return range;
    }

    // ----------------- misc helpers -----------------

    private static bool CanBeNull(Type t) => !t.IsValueType || Nullable.GetUnderlyingType(t) != null;

    public static string ToCamel(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private static FacetRangeDataType InferRangeType(Type t)
    {
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return FacetRangeDataType.DateTime;
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return FacetRangeDataType.Decimal;
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return FacetRangeDataType.Number;
        return FacetRangeDataType.Number;
    }

    private static string ToDynPath(string raw) => raw.Replace("/", ".");
    private static string ToODataPath(string raw) => raw.Replace(".", "/");

    private sealed class ValueLiteral
    {
        public string Display { get; }
        public string Literal { get; }
        public ValueLiteral(string display, string literal) { Display = display; Literal = literal; }
    }

    private sealed class FacetMeta
    {
        public string Key { get; set; } = default!;
        public string Field { get; set; } = default!;
        public string ValuePathOData { get; set; } = default!;
        public string ValuePathDyn { get; set; } = default!;
        public Type ValueClr { get; set; } = typeof(string);
    }

    private static string? ResolveEnumNameSafe(Type enumType, object? value)
    {
        if (value is null) return null;

        if (value is string s)
        {
            if (Enum.TryParse(enumType, s, true, out object? parsed) && parsed is not null)
                return Enum.GetName(enumType, parsed);
            return null;
        }

        if (value.GetType().IsEnum && value.GetType() == enumType)
            return Enum.GetName(enumType, value);

        try
        {
            var underlying = Enum.GetUnderlyingType(enumType);
            var numeric = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
            return Enum.GetName(enumType, numeric!);
        }
        catch { return null; }
    }
}
#endif