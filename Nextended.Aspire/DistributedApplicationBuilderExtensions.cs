using Aspire.Hosting;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Nextended.Core.Extensions;

namespace Nextended.Aspire;

/// <summary>
/// Extension methods for <see cref="IResourceBuilder{T}"/> that provide conditional operations such as waiting for dependencies
/// and adding references or environment variables to a resource.
/// </summary>
public static partial class DistributedApplicationBuilderExtensions
{
    [GeneratedRegex("(?=[A-Z]+)")]
    private static partial Regex PascalCaseRegex();

    [GeneratedRegex("[^a-zA-Z0-9-]")]
    private static partial Regex SpecialCharsRegex();

    [GeneratedRegex("(?<=-)-+")]
    private static partial Regex MultipleDashRegex();

    public static IResourceBuilder<ProjectResource> AddWithAutoNaming<TProject>(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string? name = null,
        string? launchProfileName = null
    ) where TProject : IProjectMetadata, new()
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(launchProfileName))
        {
            string projectName = ProjectName<TProject>();
            name ??= EscapeProjectname(projectName);
            launchProfileName ??= projectName;
        }

        return builder.AddProject<TProject>(name, launchProfileName);
    }

    private static string EscapeProjectname(string projectName)
    {
        var name = PascalCaseRegex()
            .Replace(projectName, "-");

        name = SpecialCharsRegex()
            .Replace(name, "-");

        name = MultipleDashRegex()
            .Replace(name, "");

        name = name.ToLowerInvariant()
            .Trim('-');

        return name;
    }

    private static string ProjectName<TProject>() where TProject : IProjectMetadata, new() => new TProject().ProjectName();
    private static string ProjectName(this IProjectMetadata project) => Path.GetFileNameWithoutExtension(project.ProjectPath);

    public static IResourceBuilder<T> WaitForIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource>? dependency) where T : IResourceWithWaitSupport
    {
        return builder.WaitForIf(dependency is not null, dependency!);
    }

    /// <summary>
    /// Waits for the specified dependency if it exists and its resource implements <see cref="IResourceWithParent"/>.
    /// Otherwise, the original builder is returned.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports waiting.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="dependency">The dependency resource builder to wait for.</param>
    /// <returns>
    /// The extended resource builder that waits for the dependency if applicable,
    /// otherwise the original builder.
    /// </returns>
    public static IResourceBuilder<T> WaitForIfResourceWithParent<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource>? dependency) where T : IResourceWithWaitSupport
    {
        return builder.WaitForIf(dependency is { Resource: IResourceWithParent }, dependency!);
    }

    public static IResourceBuilder<T> WaitForIf<T>(this IResourceBuilder<T> builder, bool condition, IResourceBuilder<IResource> dependency) where T : IResourceWithWaitSupport
    {
        return condition? builder.WaitFor(dependency) : builder;
    }

    public static IResourceBuilder<T> WaitForCompletionIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource>? dependency) where T : IResourceWithWaitSupport
    {
        return builder.WaitForCompletionIf(dependency is not null, dependency!);
    }

    /// <summary>
    /// Waits for the completion of the specified dependency if it exists and its resource implements <see cref="IResourceWithParent"/>.
    /// Otherwise, the original builder is returned.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports waiting.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="dependency">The dependency resource builder whose completion should be awaited.</param>
    /// <returns>
    /// The extended resource builder that waits for the dependency's completion if applicable,
    /// otherwise the original builder.
    /// </returns>
    public static IResourceBuilder<T> WaitForCompletionIfResourceWithParent<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource>? dependency) where T : IResourceWithWaitSupport
    {
        return builder.WaitForCompletionIf(dependency is { Resource: IResourceWithParent }, dependency!);
    }

    public static IResourceBuilder<T> WaitForCompletionIf<T>(this IResourceBuilder<T> builder, bool condition, IResourceBuilder<IResource> dependency) where T : IResourceWithWaitSupport
    {
        return condition ? builder.WaitForCompletion(dependency) : builder;
    }

    public static IResourceBuilder<T> WithEnvironments<T, TObject>(this IResourceBuilder<T> builder, TObject options) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironments(options, typeof(TObject).Name);
    }

    public static IResourceBuilder<T> WithEnvironments<T, TObject>(this IResourceBuilder<T> builder, TObject options, string prefix) where T : IResourceWithEnvironment
    {
        var flatDictionary = options.ToFlatDictionary("__");
        flatDictionary?.Apply(kv => builder.WithEnvironment($"{prefix}__{kv.Key.Replace(".", "__")}", kv.Value));
        return builder;
    }

    public static IResourceBuilder<T> WithEnvironmentsIf<T, TObject>(this IResourceBuilder<T> builder, bool condition, TObject options, string prefix) where T : IResourceWithEnvironment
    {
        return condition ? builder.WithEnvironments(options, prefix) : builder;
    }

    public static IResourceBuilder<T> WithEnvironmentsIf<T, TObject>(this IResourceBuilder<T> builder, bool condition, TObject options) where T : IResourceWithEnvironment
    {
        return condition ? builder.WithEnvironments(options) : builder;
    }

    public static IResourceBuilder<T> WithEnvironmentsIf<T, TObject>(this IResourceBuilder<T> builder, TObject options, string prefix) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironmentsIf(options != null, options, prefix);

    }

    public static IResourceBuilder<T> WithEnvironmentsIf<T, TObject>(this IResourceBuilder<T> builder, TObject options) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironmentsIf(options != null, options);
    }


    /// <summary>
    /// Adds a reference to the environment if the specified dependency (with a connection string) is not null.
    /// If the dependency is null, the builder is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="dependency">The dependency resource builder containing a connection string.</param>
    /// <returns>
    /// The extended resource builder with the reference added if applicable,
    /// otherwise the original builder.
    /// </returns>
    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResourceWithConnectionString>? dependency) where T : IResourceWithEnvironment
    {
        return dependency != null ? builder.WithReference(dependency) : builder;
    }

    /// <summary>
    /// Adds a reference to the environment if the specified dependency (with service discovery) is not null.
    /// If the dependency is null, the builder is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="dependency">The dependency resource builder containing service discovery information.</param>
    /// <returns>
    /// The extended resource builder with the reference added if applicable,
    /// otherwise the original builder.
    /// </returns>
    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResourceWithServiceDiscovery>? dependency) where T : IResourceWithEnvironment
    {
        return dependency != null ? builder.WithReference(dependency) : builder;
    }

    /// <summary>
    /// Adds a reference to the environment using the provided name and URI if the URI is not null.
    /// If the URI is null, the builder is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="name">The name of the reference.</param>
    /// <param name="uri">The URI for the reference. If null, no reference is added.</param>
    /// <returns>
    /// The extended resource builder with the reference added if applicable,
    /// otherwise the original builder.
    /// </returns>
    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, string name, Uri? uri) where T : IResourceWithEnvironment
    {
        return uri != null ? builder.WithReference(name, uri) : builder;
    }

    /// <summary>
    /// Adds a reference to the environment using the provided <see cref="EndpointReference"/> if it is not null.
    /// If the reference is null, the builder is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="dependency">The endpoint reference for the dependency.</param>
    /// <returns>
    /// The extended resource builder with the reference added if applicable,
    /// otherwise the original builder.
    /// </returns>
    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, EndpointReference? dependency) where T : IResourceWithEnvironment
    {
        return dependency != null ? builder.WithReference(dependency) : builder;
    }

    /// <summary>
    /// Adds an environment variable using a key derived from the caller's expression.
    /// The specified string value is assigned to the derived key.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <typeparam name="TTarget">The target type used to derive the key.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="keyExpression">An expression that indicates the key for the environment variable.</param>
    /// <param name="value">The string value to assign to the environment variable.</param>
    /// <param name="caller">The caller expression used to derive the key, automatically provided by the compiler.</param>
    /// <returns>The modified resource builder with the environment variable added.</returns>
    public static IResourceBuilder<T> WithEnvironment<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, object> keyExpression, string value, [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        var key = string.Join("__", caller.Split('.').Skip(1).ToArray());
        return builder.WithEnvironment(key, value);
    }

    /// <summary>
    /// Adds an environment variable using a key derived from the caller's expression.
    /// The specified <see cref="EndpointReference"/> value is assigned to the derived key.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <typeparam name="TTarget">The target type used to derive the key.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="keyExpression">An expression that indicates the key for the environment variable.</param>
    /// <param name="value">The endpoint reference to assign to the environment variable.</param>
    /// <param name="caller">The caller expression used to derive the key, automatically provided by the compiler.</param>
    /// <returns>The modified resource builder with the environment variable added.</returns>
    public static IResourceBuilder<T> WithEnvironment<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, object> keyExpression, EndpointReference value, [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        var key = string.Join("__", caller.Split('.').Skip(1).ToArray());
        return builder.WithEnvironment(key, value);
    }
    
    public static IResourceBuilder<T> WaitForIf<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResource>[]? dependencies)
        where T : IResourceWithWaitSupport
    {
        if (dependencies != null)
        {
            foreach (var dep in dependencies)
                builder.WaitForIf(dep);
        }
        return builder;
    }

    public static IResourceBuilder<T> WaitForCompletionIf<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResource>[]? dependencies)
        where T : IResourceWithWaitSupport
    {
        if (dependencies != null)
        {
            foreach (var dep in dependencies)
                builder.WaitForCompletionIf(dep);
        }
        return builder;
    }

    public static IResourceBuilder<T> WithReferencesIf<T>(this IResourceBuilder<T> builder, params IResourceBuilder<IResourceWithConnectionString>[]? dependencies)
        where T : IResourceWithEnvironment
    {
        if (dependencies != null)
        {
            foreach (var dep in dependencies)
                builder.WithReferenceIf(dep);
        }
        return builder;
    }

    public static IResourceBuilder<T> WithReferencesIf<T>(this IResourceBuilder<T> builder, params IResourceBuilder<IResourceWithServiceDiscovery>[]? dependencies)
        where T : IResourceWithEnvironment
    {
        if (dependencies != null)
        {
            foreach (var dep in dependencies)
                builder.WithReferenceIf(dep);
        }
        return builder;
    }

    public static IResourceBuilder<T> WithExplicitStartIf<T>(this IResourceBuilder<T> builder, bool condition) where T : IResource
    {
        if (condition)
            builder.WithExplicitStart();
        return builder;
    }

}