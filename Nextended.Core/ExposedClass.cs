using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Nextended.Core
{
    /// <summary>
    /// Provides access to static members of a class including private members using a dynamic object
    /// </summary>
    public class ExposedClass : DynamicObject
    {
        private readonly Type classType;
        private readonly Dictionary<string, Dictionary<int, List<MethodInfo>>> staticMethods;
        private readonly Dictionary<string, Dictionary<int, List<MethodInfo>>> staticGenericMethods;

        private ExposedClass(Type type)
        {
            classType = type;

            staticMethods =
                classType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Where(m => !m.IsGenericMethod)
                    .GroupBy(m => m.Name)
                    .ToDictionary(
                        p => p.Key,
                        p => p.GroupBy(r => r.GetParameters().Length).ToDictionary(r => r.Key, r => r.ToList()));

            staticGenericMethods =
                classType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.IsGenericMethod)
                    .GroupBy(m => m.Name)
                    .ToDictionary(
                        p => p.Key,
                        p => p.GroupBy(r => r.GetParameters().Length).ToDictionary(r => r.Key, r => r.ToList()));
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            // Get type args of the call
            Type[] typeArgs = ExposedObjectHelper.GetTypeArgs(binder);
            if (typeArgs != null && typeArgs.Length == 0) typeArgs = null;

            // Try to call a non-generic instance method
            if (typeArgs == null
                    && staticMethods.ContainsKey(binder.Name)
                    && staticMethods[binder.Name].ContainsKey(args.Length)
                    && ExposedObjectHelper.InvokeBestMethod(args, null, staticMethods[binder.Name][args.Length], out result))
            {
                return true;
            }

            // Try to call a generic instance method
            if (staticMethods.ContainsKey(binder.Name)
                    && staticMethods[binder.Name].ContainsKey(args.Length))
            {
                var methods = (from method in staticGenericMethods[binder.Name][args.Length] 
							   where method.GetGenericArguments().Length == typeArgs.Length 
							   select method.MakeGenericMethod(typeArgs)).ToList();

            	if (ExposedObjectHelper.InvokeBestMethod(args, null, methods, out result))
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var propertyInfo = classType.GetProperty(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(null, value, null);
                return true;
            }

            var fieldInfo = classType.GetField(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, value);
                return true;
            }

            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var propertyInfo = classType.GetProperty(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            if (propertyInfo != null)
            {
                result = propertyInfo.GetValue(null, null);
                return true;
            }

            var fieldInfo = classType.GetField(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                result = fieldInfo.GetValue(null);
                return true;
            }

            result = null;
            return false;
        }

        public static dynamic From(Type type)
        {
            return new ExposedClass(type);
        }
    }
}