using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nextended.Core.Extensions
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            assembly.ThrowIfNull(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static IEnumerable<MethodInfo> AsyncVoidMethods(this Assembly assembly)
        {
            return assembly.GetLoadableTypes()
                .SelectMany(type => type.GetMethods(
                    BindingFlags.NonPublic
                    | BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly))
                .Where(method => method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                .Where(method => method.ReturnType == typeof(void));
        }
    }
}