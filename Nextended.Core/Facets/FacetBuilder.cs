#if !NETSTANDARD

using Microsoft.Extensions.DependencyInjection;
using Nextended.Core.Attributes;
using Nextended.Core.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Facets;

/// <summary>
/// Builds facet definitions (groups/options/ranges) for a given IQueryable.
/// - Reflection metadata is cached per type (ConcurrentDictionary)
/// - All list facets are fetched in a single combined DB query
/// - Range preset counts use a single-roundtrip IIF/GroupBy query per group
/// - Parallel range computation available via FacetBuilderOptions.ParallelizeRanges
/// - Disjunctive faceting controlled by options and GroupOperator
/// </summary>
[RegisterAs(typeof(IFacetBuilder), RegisterAsImplementation = true)]
public sealed class FacetBuilder(
    IServiceScopeFactory scopeFactory,
    FacetBuilderOptions? options = null,
    IODataLiteralFormatter? literal = null
) : IFacetBuilder
{
    private FacetBuilderOptions _options = options ?? new FacetBuilderOptions();
    private readonly IODataLiteralFormatter _literal = literal ?? new DefaultODataLiteralFormatter();
    private Func<string, Type?, string>? _localizerFn;

    public IFacetBuilder WithLocalizationFunc(Func<string, Type?, string> localizerFn)
        => this.SetProperties(b => b._localizerFn = localizerFn);

    public Task<List<FacetGroup>> BuildAsync<T>(
        Func<IServiceProvider, Task<IQueryable<T>>> baseQueryFactory,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct = default) where T : class => BuildInternalAsync(baseQueryFactory, applied, ct);

    public IFacetBuilder WithOptions(FacetBuilderOptions options)
        => this.SetProperties(b => b._options = options);

    // ── Public API ──────────────────────────────────────────────────────────

    public Task<List<FacetGroup>> BuildAsync<T>(
        IQueryable<T> alreadyFilteredQuery,
        CancellationToken ct = default) where T : class
        => BuildCoreAsync(alreadyFilteredQuery, baseQueryFactory: null, applied: [], allowParallelDb: false, ct);

    public Task<List<FacetGroup>> BuildAsync<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct = default) where T : class
        => BuildCoreAsync(baseQuery, baseQueryFactory: null, applied, allowParallelDb: false, ct);

    /// <summary>
    /// Factory overload: creates a fresh query per parallel range-count scope.
    /// Enables <see cref="FacetBuilderOptions.ParallelizeRanges"/> when a factory is supplied.
    /// </summary>
    public Task<List<FacetGroup>> BuildAsync<T>(
        Func<Task<IQueryable<T>>> baseQueryFactory,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct = default) where T : class
    {
        return BuildAsync(_ => baseQueryFactory(), applied, ct);
    }
    
    // ── Type-level reflection cache ─────────────────────────────────────────

    private static readonly ConcurrentDictionary<Type, FacetTypeCache> _facetCache = new();

    private static FacetTypeCache GetFacetCache(Type t)
        => _facetCache.GetOrAdd(t, static type =>
        {
            var props = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .SelectMany(p => p.GetCustomAttributes<ProvideFacetAttribute>(true)
                                  .Select(facet => (Prop: p, Facet: facet)))
                .OrderBy(x => x.Facet.Order)
                .ToArray();

            return new FacetTypeCache(props);
        });

    // ── Core build ──────────────────────────────────────────────────────────

    private async Task<List<FacetGroup>> BuildInternalAsync<T>(
        Func<IServiceProvider, Task<IQueryable<T>>> baseQueryFactory,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct) where T : class
    {
        using var scope = scopeFactory.CreateScope();
        
        var baseQuery = await baseQueryFactory(scope.ServiceProvider);
        

        return await BuildCoreAsync(baseQuery, baseQueryFactory, applied, allowParallelDb: _options.ParallelizeRanges, ct);
    }

    private async Task<List<FacetGroup>> BuildCoreAsync<T>(
        IQueryable<T> baseQuery,
        Func<IServiceProvider, Task<IQueryable<T>>>? baseQueryFactory,
        IReadOnlyList<AppliedFacet> applied,
        bool allowParallelDb,
        CancellationToken ct) where T : class
    {
        var overallSw = Stopwatch.StartNew();
        long dbMs = 0, nonDbMs = 0;

        var typeCache = GetFacetCache(typeof(T));
        if (typeCache.Props.Length == 0) return [];

        var metaSw = Stopwatch.StartNew();
        var metaByKey = BuildMetaByKey(typeCache);
        metaSw.Stop();
        nonDbMs += metaSw.ElapsedMilliseconds;

        if (metaByKey.Count == 0) return [];

        // ── 1. Fetch all list-facet counts in one combined query ─────────────
        List<FacetResult> facetResults = [];
        Dictionary<string, List<FacetResult>> groupedFacetResults = new(StringComparer.OrdinalIgnoreCase);

        if (_options.ComputeDynamicLists)
        {
            var listMetas = metaByKey.Values
                .Where(m => m.Attribute.Type is FacetType.CheckboxList or FacetType.TokenList or FacetType.Radio)
                .ToArray();

            if (listMetas.Length > 0)
            {
                var listDbSw = Stopwatch.StartNew();

                IQueryable<FacetResult>? combined = null;
                foreach (var m in listMetas)
                {
                    var q = BuildFacetQuery(m, baseQuery, applied, metaByKey);
                    combined = combined == null ? q : combined.Concat(q);
                }

                //facetResults = await combined!
                //    .AsNoTracking()
                //    .ToListAsync(ct);

                facetResults = combined!.ToList();

                groupedFacetResults = facetResults
                    .GroupBy(fr => fr.Discriminator, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                listDbSw.Stop();
                dbMs += listDbSw.ElapsedMilliseconds;
            }
        }

        // ── 2. Build list-group DTOs (CPU, parallelised) ────────────────────
        var cpuSw = Stopwatch.StartNew();

        var listBuildTasks = metaByKey.Values
            .Where(m => m.Attribute.Type is FacetType.CheckboxList or FacetType.TokenList or FacetType.Radio)
            .Select(m => Task.Run(() =>
            {
                var groupSw = Stopwatch.StartNew();
                var group = m.FacetGroup;

                if (_options.ComputeDynamicLists &&
                    groupedFacetResults.TryGetValue(group.Key, out var rows))
                {
                    foreach (var row in rows)
                    {
                        var vl = ToValueLiteral(row.Value, m.ValueClr);
                        group.Options.Add(new FacetOption
                        {
                            Value = vl.Display,
                            Label = L(row.Label ?? vl.Display, m.ScalarType),
                            Count = Convert.ToInt64(row.Count, CultureInfo.InvariantCulture),
                            Enabled = true,
                            OData = $"{group.ValuePath} eq {vl.Literal}"
                        });
                    }

                    IncludeSelectedEvenIfMissingFast(group, applied, m.ScalarType);
                }

                groupSw.Stop();
                group.BuildDuration = groupSw.Elapsed;
            }, ct));

        await Task.WhenAll(listBuildTasks);

        cpuSw.Stop();
        nonDbMs += cpuSw.ElapsedMilliseconds;

        // ── 3. Range facets ──────────────────────────────────────────────────
        if (allowParallelDb && baseQueryFactory != null && _options.ComputeDynamicLists)
        {
            var rangeDbMs = await ComputeRangesParallelAsync(baseQueryFactory, applied, metaByKey, ct);
            dbMs += rangeDbMs;
        }
        else
        {
            foreach (var m in metaByKey.Values)
            {
                if (m.Attribute.Type is not (FacetType.Range or FacetType.DateRange))
                    continue;

                var group = m.FacetGroup;
                var groupTotalSw = Stopwatch.StartNew();
                long groupDbLocalMs = 0;

                group.Range = DefaultDateOrNumberRange(group.ValuePath, m.ValueClr);

                if (_options.ComputeDynamicLists && group.Range?.Presets.Any() == true)
                {
                    var dynPath = ToDynPath(group.ValuePath);
                    var presets = group.Range.Presets;
                    var validRanges = new List<(int Idx, DateTimeOffset From, DateTimeOffset To)>();

                    for (int i = 0; i < presets.Count; i++)
                    {
                        if (DateTimeOffset.TryParse(presets[i].Value.From, out var from) &&
                            DateTimeOffset.TryParse(presets[i].Value.To, out var to))
                            validRanges.Add((i, from, to));
                    }

                    if (validRanges.Count > 0)
                    {
                        var presetDbSw = Stopwatch.StartNew();

                        var ctx = ApplyAppliedFiltersExceptGroup(baseQuery, applied, group.Key, metaByKey); //.AsNoTracking();

                        var ranges = validRanges.Select(r => (r.From, r.To)).ToArray();
                        var counts = await CountRangesDynamicAsync(ctx, dynPath, ranges, ct);

                        for (int i = 0; i < validRanges.Count; i++)
                            presets[validRanges[i].Idx].Count = counts[i];

                        presetDbSw.Stop();
                        groupDbLocalMs += presetDbSw.ElapsedMilliseconds;
                    }
                }

                groupTotalSw.Stop();
                group.BuildDuration = groupTotalSw.Elapsed;
                dbMs += groupDbLocalMs;
                nonDbMs += Math.Max(0, groupTotalSw.ElapsedMilliseconds - groupDbLocalMs);
            }
        }

        overallSw.Stop();
        System.Diagnostics.Debug.WriteLine(
            $"[FacetBuilder] total={overallSw.ElapsedMilliseconds} ms " +
            $"(db={dbMs} ms, non-db={nonDbMs} ms), facets={metaByKey.Count}");

        return metaByKey.Values.Select(m => m.FacetGroup).ToList();
    }

    // ── Parallel range computation ───────────────────────────────────────────

    private async Task<long> ComputeRangesParallelAsync<T>(
        Func<IServiceProvider, Task<IQueryable<T>>> baseQueryFactory,
        IReadOnlyList<AppliedFacet> applied,
        IReadOnlyDictionary<string, FacetMeta> metaByKey,
        CancellationToken ct) where T : class
    {
        var rangeMetas = metaByKey.Values
            .Where(m => m.Attribute.Type is FacetType.Range or FacetType.DateRange)
            .ToArray();

        if (rangeMetas.Length == 0) return 0;

        var maxPar = Math.Max(1, _options.MaxDbParallelism);
        using var gate = new SemaphoreSlim(maxPar);
        var counts = new ConcurrentDictionary<string, long[]>(StringComparer.OrdinalIgnoreCase);
        long totalDbMs = 0;

        var tasks = rangeMetas.Select(async m =>
        {
            var group = m.FacetGroup;
            var groupSw = Stopwatch.StartNew();

            group.Range = DefaultDateOrNumberRange(group.ValuePath, m.ValueClr);
            if (group.Range?.Presets == null || group.Range.Presets.Count == 0)
            {
                group.BuildDuration = groupSw.Elapsed;
                return;
            }

            var presets = group.Range.Presets;
            var ranges = new (DateTimeOffset From, DateTimeOffset To)[presets.Count];
            bool allParsed = true;

            for (int i = 0; i < presets.Count; i++)
            {
                if (!DateTimeOffset.TryParse(presets[i].Value.From, out var from) ||
                    !DateTimeOffset.TryParse(presets[i].Value.To, out var to))
                { allParsed = false; break; }
                ranges[i] = (from, to);
            }

            if (!allParsed) { group.BuildDuration = groupSw.Elapsed; return; }

            await gate.WaitAsync(ct);
            try
            {
                using var scope = scopeFactory.CreateScope();

                var dbSw = Stopwatch.StartNew();

                var q = await baseQueryFactory(scope.ServiceProvider);
                var ctx = ApplyAppliedFiltersExceptGroup(q, applied, group.Key, metaByKey);//.AsNoTracking();

                var dynPath = ToDynPath(group.ValuePath);

                var arr = await CountRangesDynamicAsync(ctx, dynPath, ranges, ct);


                dbSw.Stop();
                Interlocked.Add(ref totalDbMs, dbSw.ElapsedMilliseconds);


                counts[group.Key] = arr;

                groupSw.Stop();
                group.BuildDuration = groupSw.Elapsed;
            }
            finally
            {
                gate.Release();
            }

            groupSw.Stop();
            group.BuildDuration = groupSw.Elapsed;
        });

        await Task.WhenAll(tasks);

        foreach (var m in rangeMetas)
        {
            var g = m.FacetGroup;
            if (g.Range?.Presets == null || !counts.TryGetValue(g.Key, out var arr)) continue;
            for (int i = 0; i < g.Range.Presets.Count && i < arr.Length; i++)
                g.Range.Presets[i].Count = arr[i];
        }

        return totalDbMs;
    }

    // ── Single-roundtrip range counting ─────────────────────────────────────

    private static async Task<long[]> CountRangesDynamicAsync<T>(
        IQueryable<T> q,
        string dynPath,
        (DateTimeOffset From, DateTimeOffset To)[] ranges,
        CancellationToken ct)
    {
        if (ranges.Length == 0) return Array.Empty<long>();

        var parts = new string[ranges.Length];
        var args = new object[ranges.Length * 2];

        for (int i = 0; i < ranges.Length; i++)
        {
            int p = i * 2;
            parts[i] = $"Sum(IIF(({dynPath} >= @{p} && {dynPath} < @{p + 1}), 1, 0)) as C{i}";
            args[p] = ranges[i].From;
            args[p + 1] = ranges[i].To;
        }

        var select = "new (" + string.Join(", ", parts) + ")";
        var dynQuery = q.GroupBy("1").Select(select, args);

        var rows = await dynQuery.ToDynamicListAsync(ct);
        if (rows.Count == 0 || rows[0] is null) return new long[ranges.Length];

        var row = rows[0];
        var t = row.GetType();
        var result = new long[ranges.Length];

        for (int i = 0; i < ranges.Length; i++)
        {
            var val = t.GetProperty($"C{i}")?.GetValue(row);
            result[i] = val == null ? 0L : Convert.ToInt64(val, CultureInfo.InvariantCulture);
        }

        return result;
    }

    // ── Metadata builder ─────────────────────────────────────────────────────

    private Dictionary<string, FacetMeta> BuildMetaByKey(FacetTypeCache typeCache)
    {
        var dict = new Dictionary<string, FacetMeta>(typeCache.Props.Length, StringComparer.OrdinalIgnoreCase);

        foreach (var (propertyInfo, attr) in typeCache.Props)
        {
            var scalarType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            var rawValuePath = string.IsNullOrWhiteSpace(attr.ValuePath) ? propertyInfo.Name : attr.ValuePath!;
            var valueClr = attr.ValueType ?? (string.IsNullOrWhiteSpace(attr.ValuePath) ? scalarType : typeof(string));
            var key = propertyInfo.Name.ToCamel();

            dict[key] = new FacetMeta
            {
                Key = key,
                Field = propertyInfo.Name,
                ValuePathDyn = ToDynPath(rawValuePath),
                LabelPathDyn = string.IsNullOrWhiteSpace(attr.LabelPath)
                                   ? ToDynPath(rawValuePath)
                                   : ToDynPath(attr.LabelPath),
                ValueClr = valueClr,
                ScalarType = scalarType,
                Attribute = attr,
                FacetGroup = new FacetGroup
                {
                    Key = key,
                    Label = L(attr.Label ?? propertyInfo.Name),
                    Field = propertyInfo.Name,
                    ValuePath = ToODataPath(rawValuePath),
                    LabelPath = string.IsNullOrWhiteSpace(attr.LabelPath) ? null : ToODataPath(attr.LabelPath),
                    ValueClrType = valueClr.AssemblyQualifiedName!,
                    Type = attr.Type,
                    GroupOperator = attr.GroupOperator,
                    MultiSelect = attr.MultiSelect,
                    Order = attr.Order,
                    Options = new List<FacetOption>()
                }
            };
        }

        return dict;
    }

    // ── Facet query builder ──────────────────────────────────────────────────

    private IQueryable<FacetResult> BuildFacetQuery<T>(
        FacetMeta meta,
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        Dictionary<string, FacetMeta> metaByKey)
    {
        var group = meta.FacetGroup;

        bool disjunctiveForGroup =
            _options.DisjunctiveFacets ||
            (_options.DisjunctiveByGroupOperator && group.GroupOperator == FacetGroupOperator.Or);

        IQueryable<T> ctxQ = disjunctiveForGroup
            ? ApplyAppliedFiltersExceptGroup(baseQuery, applied, meta.Key, metaByKey)
            : baseQuery;

        var top = meta.Attribute.TopDistinct > 0
            ? meta.Attribute.TopDistinct
            : _options.DefaultTopDistinct;

        IQueryable query = ctxQ
            .Where($"{meta.ValuePathDyn} != null")
            .GroupBy($"new ({meta.ValuePathDyn} as value, {meta.LabelPathDyn} as label)")
            .OrderBy("Count() desc");

        if (top != null) query = query.Take(top.Value);

        return query.Select<FacetResult>(
            $"""
             new (
                 "{meta.Key}" as {nameof(FacetResult.Discriminator)},
                 As(Key.value, "string") as {nameof(FacetResult.Value)},
                 As(Key.label, "string") as {nameof(FacetResult.Label)},
                 Count() as {nameof(FacetResult.Count)}
             )
             """);
    }

    // ── Disjunctive helper ───────────────────────────────────────────────────

    private IQueryable<T> ApplyAppliedFiltersExceptGroup<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        string currentGroupKey,
        IReadOnlyDictionary<string, FacetMeta> metaByKey)
    {
        if (applied.Count == 0) return baseQuery;

        var otherGroups = applied
            .Where(a => !string.Equals(a.GroupKey, currentGroupKey, StringComparison.OrdinalIgnoreCase))
            .GroupBy(a => a.GroupKey, StringComparer.OrdinalIgnoreCase);

        var q = baseQuery;

        foreach (var grp in otherGroups)
        {
            if (!metaByKey.TryGetValue(grp.Key, out var meta)) continue;

            var values = grp.SelectMany(g => g.Values)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();

            if (values.Length == 0) continue;

            var pars = new object?[values.Length];
            var ors = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                pars[i] = ParseValue(values[i], meta.ValueClr);
                ors[i] = $"{meta.ValuePathDyn} == @{i}";
            }

            q = q.Where("(" + string.Join(" OR ", ors) + ")", pars);
        }

        return q;
    }

    // ── Selected-even-if-missing (HashSet fast path) ─────────────────────────

    private void IncludeSelectedEvenIfMissingFast(
        FacetGroup group,
        IReadOnlyList<AppliedFacet> applied,
        Type scalarType)
    {
        var existing = new HashSet<string>(
            group.Options.Select(o => o.Value),
            StringComparer.OrdinalIgnoreCase);

        foreach (var chip in applied.Where(a =>
            string.Equals(a.GroupKey, group.Key, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var v in chip.Values)
            {
                if (existing.Add(v))
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

    // ── Literal / range helpers ──────────────────────────────────────────────

    private ValueLiteral ToValueLiteral(object? rawValue, Type clrType)
    {
        var display = rawValue?.ToString() ?? "";
        object? coerced = rawValue;

        try
        {
            var t = Nullable.GetUnderlyingType(clrType) ?? clrType;
            if (rawValue is not null && rawValue.GetType() != t)
                coerced = t == typeof(Guid)
                    ? Guid.Parse(display)
                    : Convert.ChangeType(rawValue, t, CultureInfo.InvariantCulture);
        }
        catch { coerced = display; }

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

        if (dt != typeof(DateTime) && dt != typeof(DateTimeOffset))
            return range;

        var now = DateTimeOffset.UtcNow;

        DateTimeOffset FromDays(int d) => now.AddDays(d);
        DateTimeOffset NextMidnight() => new(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);

        string RangeClause(object from, object to) => _options.BuildLiterals
            ? _literal.RangeClause(odataPath, from, to, dt)
            : $"({odataPath} ge {DefaultODataLiteralFormatter.ToSimpleLiteral(from, dt)}" +
              $" and {odataPath} lt {DefaultODataLiteralFormatter.ToSimpleLiteral(to, dt)})";

        FacetRangeBucket BucketForDays(int days) => new()
        {
            Key = $"last{days}d",
            Label = string.Format(L("Last {0} days"), days),
            Value = new FacetRangeValue
            {
                From = FromDays(-days).ToString("yyyy-MM-dd"),
                To = NextMidnight().ToString("yyyy-MM-dd"),
                OData = RangeClause(FromDays(-days), NextMidnight())
            }
        };

        range.Presets = [BucketForDays(7), BucketForDays(14), BucketForDays(30)];
        return range;
    }

    private static FacetRangeDataType InferRangeType(Type t)
    {
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return FacetRangeDataType.DateTime;
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return FacetRangeDataType.Decimal;
        return FacetRangeDataType.Number;
    }

    // ── Localisation ─────────────────────────────────────────────────────────

    private string L(string key, Type? type = null)
        => _localizerFn != null ? _localizerFn(key, type) : key;

    // ── Path helpers ─────────────────────────────────────────────────────────

    private static string ToDynPath(string raw) => raw.Replace("/", ".");
    private static string ToODataPath(string raw) => raw.Replace(".", "/");

    // ── Value parsing ────────────────────────────────────────────────────────

    private static object? ParseValue(string s, Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t == typeof(string)) return s;
        if (t == typeof(Guid)) return Guid.Parse(s);
        if (t == typeof(bool)) return bool.Parse(s);
        if (t.IsEnum) return Enum.Parse(t, s, ignoreCase: true);
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

    // ── Private types ────────────────────────────────────────────────────────

    private sealed record ValueLiteral(string Display, string Literal);

    private sealed record FacetMeta
    {
        public required string Key { get; init; }
        public required string Field { get; init; }
        public required string ValuePathDyn { get; init; }
        public required string LabelPathDyn { get; init; }
        public required Type ValueClr { get; init; }
        public required Type ScalarType { get; init; }
        public required ProvideFacetAttribute Attribute { get; init; }
        public required FacetGroup FacetGroup { get; init; }
    }

    private sealed record FacetResult
    {
        public string Discriminator { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string? Label { get; set; }
        public int Count { get; set; }
    }

    private sealed record FacetTypeCache((PropertyInfo Prop, ProvideFacetAttribute Facet)[] Props);
}

#endif