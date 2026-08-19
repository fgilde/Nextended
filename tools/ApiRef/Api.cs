using System.Reflection;
using System.Text;

/// <summary>Reads a compiled assembly's public surface and renders it as a documentation page.</summary>
internal static class Api
{
    internal sealed record Member(string Kind, string Signature, string DocId, string? Summary);

    internal sealed record TypeModel(
        string Namespace,
        string Name,
        string Kind,
        string? Summary,
        List<Member> Members);

    // ------------------------------------------------------------------ inspect

    internal static List<TypeModel> Inspect(
        string dllPath,
        string package,
        Dictionary<string, string> summaries,
        List<string> gaps)
    {
        // Resolve against the assembly's own folder, every other built Nextended assembly (so a
        // base type from Nextended.Core resolves while reading Nextended.Web) and the running
        // runtime. Third-party bases (EF Core, Aspire) may still be unresolvable — every metadata
        // access below therefore tolerates failure instead of aborting the run.
        var ownFolder = Path.GetDirectoryName(dllPath)!;
        var binRoot = Path.GetFullPath(Path.Combine(ownFolder, "..", ".."));

        var paths = new List<string>(Directory.GetFiles(ownFolder, "*.dll"));
        if (Directory.Exists(binRoot))
            paths.AddRange(Directory.GetFiles(binRoot, "*.dll", SearchOption.AllDirectories));
        paths.AddRange(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll"));

        // PathAssemblyResolver rejects duplicate simple names, so keep the first of each.
        paths = paths
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        using var mlc = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct()));
        var assembly = mlc.LoadFromAssemblyPath(dllPath);

        var result = new List<TypeModel>();

        foreach (var type in SafeTypes(assembly).Where(IsPublicSafe).OrderBy(t => t.Namespace).ThenBy(t => t.Name))
        {
          try
          {
            if (IsCompilerGenerated(type)) continue;

            var typeId = "T:" + DocName(type);
            summaries.TryGetValue(typeId, out var typeSummary);
            if (typeSummary is null) gaps.Add($"{package}\t{Display(type)}");

            var members = new List<Member>();

            MemberInfo[] declared;
            try
            {
                declared = type.GetMembers(BindingFlags.Public | BindingFlags.Instance |
                                           BindingFlags.Static | BindingFlags.DeclaredOnly);
            }
            catch { declared = []; }

            foreach (var m in declared)
            {
                if (m is Type) continue;                       // nested types are listed on their own
                if (IsCompilerGenerated(m)) continue;
                if (m is MethodInfo mi && (mi.IsSpecialName || IsOverride(mi))) continue;
                if (m is ConstructorInfo ci && ci.IsStatic) continue;

                string kind, docId;
                string? signature;
                try { (kind, signature, docId) = Describe(m, type); }
                catch { continue; }   // signature references a type we cannot resolve
                if (signature is null) continue;

                summaries.TryGetValue(docId, out var memberSummary);
                if (memberSummary is null) gaps.Add($"{package}\t{Display(type)}.{signature}");

                members.Add(new Member(kind, signature, docId, memberSummary));
            }

            result.Add(new TypeModel(
                type.Namespace ?? "",
                Display(type),
                KindOf(type),
                typeSummary,
                members.OrderBy(x => x.Kind).ThenBy(x => x.Signature, StringComparer.Ordinal).ToList()));
          }
          catch
          {
              // Unreadable metadata for this one type; the rest of the assembly is still fine.
          }
        }

        return result;
    }

    private static IEnumerable<Type> SafeTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }

    private static bool IsPublicSafe(Type t)
    {
        try { return t.IsPublic || (t.IsNestedPublic && (t.DeclaringType?.IsPublic ?? false)); }
        catch { return false; }
    }

    private static bool IsCompilerGenerated(MemberInfo m) =>
        m.Name.Contains('<') ||
        m.GetCustomAttributesData().Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");

    /// <summary>
    /// True for a method that overrides a base implementation. GetBaseDefinition() throws under a
    /// MetadataLoadContext, so this reads the metadata flags instead: an override is virtual but
    /// does not introduce a new slot. Deliberately avoids touching BaseType, which throws when the
    /// base lives in an assembly the resolver cannot find (EF Core, ASP.NET Core, Aspire).
    /// </summary>
    private static bool IsOverride(MethodInfo mi) =>
        mi.IsVirtual && !mi.IsAbstract && (mi.Attributes & MethodAttributes.NewSlot) == 0;

    /// <summary>
    /// Classifies a type. IsEnum and IsValueType resolve the base type internally and throw when it
    /// lives in an assembly the resolver cannot find, so the whole classification is guarded.
    /// </summary>
    private static string KindOf(Type t)
    {
        try
        {
            return t.IsEnum ? "enum"
                : t.IsInterface ? "interface"
                : t.IsValueType ? "struct"
                : IsDelegate(t) ? "delegate"
                : t.IsAbstract && t.IsSealed ? "static class"
                : t.IsAbstract ? "abstract class"
                : "class";
        }
        catch { return "class"; }
    }

    /// <summary>
    /// BaseType throws when the base type is in an unresolvable assembly, so a failure here just
    /// means "not a delegate" rather than aborting the whole run.
    /// </summary>
    private static bool IsDelegate(Type t)
    {
        try { return t.BaseType?.FullName is "System.MulticastDelegate" or "System.Delegate"; }
        catch { return false; }
    }

    // ------------------------------------------------------------------ signatures

    private static (string Kind, string? Signature, string DocId) Describe(MemberInfo m, Type owner) => m switch
    {
        ConstructorInfo c =>
            ("1 constructor", $"{StripGeneric(owner.Name)}({Parameters(c.GetParameters())})",
             $"M:{DocName(owner)}.#ctor({DocParams(c.GetParameters())})"),

        MethodInfo mi =>
            (mi.IsStatic && IsExtension(mi) ? "2 extension method" : "3 method",
             $"{mi.Name}{Generics(mi)}({Parameters(mi.GetParameters(), IsExtension(mi))}) : {Short(mi.ReturnType)}",
             $"M:{DocName(owner)}.{mi.Name}{(mi.IsGenericMethod ? "``" + mi.GetGenericArguments().Length : "")}" +
             $"({DocParams(mi.GetParameters())})"),

        PropertyInfo p =>
            ("4 property", $"{p.Name} : {Short(p.PropertyType)} {{ {Accessors(p)} }}",
             $"P:{DocName(owner)}.{p.Name}"),

        FieldInfo f when IsEnumSafe(owner) =>
            ("5 enum value", f.Name, $"F:{DocName(owner)}.{f.Name}"),

        FieldInfo f =>
            ("5 field", $"{f.Name} : {Short(f.FieldType)}", $"F:{DocName(owner)}.{f.Name}"),

        EventInfo e =>
            ("6 event", $"{e.Name} : {Short(e.EventHandlerType!)}", $"E:{DocName(owner)}.{e.Name}"),

        _ => ("", null, ""),
    };

    private static bool IsEnumSafe(Type t)
    {
        try { return t.IsEnum; }
        catch { return false; }
    }

    private static bool IsExtension(MethodInfo mi) =>
        mi.GetCustomAttributesData().Any(a => a.AttributeType.Name == "ExtensionAttribute");

    private static string Accessors(PropertyInfo p)
    {
        var parts = new List<string>();
        if (p.GetGetMethod() is not null) parts.Add("get;");
        if (p.GetSetMethod() is not null) parts.Add("set;");
        return parts.Count > 0 ? string.Join(' ', parts) : "get;";
    }

    private static string Parameters(ParameterInfo[] ps, bool isExtension = false) =>
        string.Join(", ", ps.Select((p, i) =>
            (isExtension && i == 0 ? "this " : "") +
            (p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "") +
            Short(p.ParameterType) + " " + p.Name +
            (p.HasDefaultValue ? " = " + Literal(p.RawDefaultValue) : "")));

    private static string Literal(object? v) => v switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        bool b => b ? "true" : "false",
        _ => v.ToString() ?? "null",
    };

    private static string Generics(MethodInfo mi) =>
        mi.IsGenericMethod ? "<" + string.Join(", ", mi.GetGenericArguments().Select(a => a.Name)) + ">" : "";

    private static string StripGeneric(string name)
    {
        var tick = name.IndexOf('`');
        return tick < 0 ? name : name[..tick];
    }

    /// <summary>Short, readable type name: <c>IEnumerable&lt;string&gt;</c> rather than the full metadata name.</summary>
    private static string Short(Type t)
    {
        if (t.IsByRef) return Short(t.GetElementType()!);
        if (t.IsArray) return Short(t.GetElementType()!) + "[]";

        if (t.IsGenericType)
        {
            var args = t.GetGenericArguments();
            var def = StripGeneric(t.Name);
            if (def == "Nullable" && args.Length == 1) return Short(args[0]) + "?";
            return def + "<" + string.Join(", ", args.Select(Short)) + ">";
        }

        return t.FullName switch
        {
            "System.Void" => "void",
            "System.String" => "string",
            "System.Boolean" => "bool",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.Decimal" => "decimal",
            "System.Double" => "double",
            "System.Single" => "float",
            "System.Object" => "object",
            "System.Byte" => "byte",
            "System.Guid" => "Guid",
            _ => StripGeneric(t.Name),
        };
    }

    private static string Display(Type t)
    {
        var name = StripGeneric(t.Name);
        return t.IsGenericType
            ? name + "<" + string.Join(", ", t.GetGenericArguments().Select(a => a.Name)) + ">"
            : name;
    }

    private static string DocName(Type t) => (t.Namespace is null ? "" : t.Namespace + ".") + t.Name.Replace('+', '.');

    private static string DocParams(ParameterInfo[] ps) =>
        string.Join(",", ps.Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));

    // ------------------------------------------------------------------ render

    private static readonly Dictionary<string, string> HeadingDe = new()
    {
        ["1 constructor"] = "Konstruktoren",
        ["2 extension method"] = "Extension Methods",
        ["3 method"] = "Methoden",
        ["4 property"] = "Eigenschaften",
        ["5 field"] = "Felder",
        ["5 enum value"] = "Werte",
        ["6 event"] = "Ereignisse",
    };

    private static readonly Dictionary<string, string> HeadingEn = new()
    {
        ["1 constructor"] = "Constructors",
        ["2 extension method"] = "Extension methods",
        ["3 method"] = "Methods",
        ["4 property"] = "Properties",
        ["5 field"] = "Fields",
        ["5 enum value"] = "Values",
        ["6 event"] = "Events",
    };

    internal static string Render(List<TypeModel> types, string package, string slug, string lang)
    {
        var de = lang == "de";
        var headings = de ? HeadingDe : HeadingEn;
        var sb = new StringBuilder();

        var title = de ? $"{package} — API-Referenz" : $"{package} — API reference";
        var intro = de
            ? $"Die vollständige öffentliche Oberfläche von `{package}`, erzeugt aus der gebauten Assembly."
            : $"The complete public surface of `{package}`, generated from the compiled assembly.";
        var other = de
            ? $"🇬🇧 [This page in English](/projects/{slug}-api)"
            : $"🇩🇪 [Diese Seite auf Deutsch](/de/projects/{slug}-api)";
        var back = de
            ? $"↩ [Zurück zur Paketseite](/de/projects/{slug})"
            : $"↩ [Back to the package page](/projects/{slug})";
        var noteText = de
            ? "Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt "
              + "auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten."
            : "This page is generated by `tools/ApiRef` from the compiled assembly — it includes members "
              + "with no XML comment and therefore cannot drift from the code. Do not edit by hand.";
        var noteTitle = de ? "Generiert" : "Generated";
        var undocumented = de ? "_Keine Beschreibung._" : "_No description._";

        sb.AppendLine("---");
        sb.AppendLine($"title: {title}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine(other);
        sb.AppendLine();
        sb.AppendLine(intro);
        sb.AppendLine();
        sb.AppendLine($"::: info {noteTitle}");
        sb.AppendLine(noteText);
        sb.AppendLine(":::");
        sb.AppendLine();
        sb.AppendLine(back);
        sb.AppendLine();

        foreach (var byNs in types.GroupBy(t => t.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"## {byNs.Key}");
            sb.AppendLine();

            foreach (var t in byNs)
            {
                // Backticks are required, not cosmetic: a bare generic in a heading
                // (### SuperType<TType, TId>) is parsed as an unclosed HTML tag by the Vue compiler.
                sb.AppendLine($"### `{t.Name}`");
                sb.AppendLine();
                sb.AppendLine($"`{t.Kind}`");
                sb.AppendLine();
                sb.AppendLine(t.Summary ?? undocumented);
                sb.AppendLine();

                foreach (var group in t.Members.GroupBy(x => x.Kind).OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    if (!headings.TryGetValue(group.Key, out var heading)) continue;
                    sb.AppendLine($"**{heading}**");
                    sb.AppendLine();
                    foreach (var mem in group)
                    {
                        sb.AppendLine($"- `{mem.Signature}`");
                        if (mem.Summary is not null) sb.AppendLine($"  <br>{mem.Summary}");
                    }
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine(back);
        return sb.ToString();
    }
}
