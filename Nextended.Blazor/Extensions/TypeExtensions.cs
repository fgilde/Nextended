using Microsoft.AspNetCore.Components;

namespace Nextended.Blazor.Extensions;

public static class TypeExtensions
{
    public static bool IsEventCallback(this Type type) => (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>) || type == typeof(EventCallback));
    public static bool IsRenderFragment(this Type type) => (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>) || type == typeof(RenderFragment));
}