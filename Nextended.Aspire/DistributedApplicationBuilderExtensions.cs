using System.Runtime.CompilerServices;

namespace Nextended.Aspire;

public static partial class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<T> WaitForIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource>? dependency) where T : IResourceWithWaitSupport
    {
        return dependency is {Resource: IResourceWithParent} ? builder.WaitFor(dependency) : builder;
    }

    public static IResourceBuilder<T> WaitForCompletionIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource>? dependency) where T : IResourceWithWaitSupport
    {
        return dependency is { Resource: IResourceWithParent } ? builder.WaitForCompletion(dependency) : builder;
    }

    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResourceWithConnectionString>? dependency) where T : IResourceWithEnvironment
    {
        return dependency != null ? builder.WithReference(dependency) : builder;
    }

    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResourceWithServiceDiscovery>? dependency) where T : IResourceWithEnvironment
    {
        return dependency != null ? builder.WithReference(dependency) : builder;
    }

    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, string name, Uri? uri) where T : IResourceWithEnvironment
    {
        return uri != null ? builder.WithReference(name, uri) : builder;
    }

    public static IResourceBuilder<T> WithReferenceIf<T>(this IResourceBuilder<T> builder, EndpointReference? dependency) where T : IResourceWithEnvironment
    {
        return dependency != null ? builder.WithReference(dependency) : builder;
    }

    public static IResourceBuilder<T> WithEnvironment<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, object> keyExpression, string value, [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        var key = string.Join("__", caller.Split('.').Skip(1).ToArray());        
        return builder.WithEnvironment(key, value);        
    }

    public static IResourceBuilder<T> WithEnvironment<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, object> keyExpression, EndpointReference value, [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        var key = string.Join("__", caller.Split('.').Skip(1).ToArray());
        return builder.WithEnvironment(key, value);        
    }

    public static IResourceBuilder<T> WithEndpointAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, object> keyExpression, IResourceBuilder<IResourceWithEndpoints>? reference, string endpointName = "http", [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        if (reference != null)
        {
            var endpointRef = reference?.GetEndpoint(endpointName);
            if (endpointRef != null)
                return builder.WithEnvironment(keyExpression, endpointRef, caller);
        }

        return builder;
    }

}
