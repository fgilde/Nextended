using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Nextended.Core
{
	/// <summary>
	/// ExposedObject easy to use private members in dynamic object
	/// </summary>
	public class ExposedObject : DynamicObject
	{
		private readonly object objectInstance;
		private readonly Type objectType;
		private readonly Dictionary<string, Dictionary<int, List<MethodInfo>>> instanceMethods;
		private readonly Dictionary<string, Dictionary<int, List<MethodInfo>>> genInstanceMethods;

		private ExposedObject(object obj)
		{
			objectInstance = obj;
			objectType = obj.GetType();

			instanceMethods =
				objectType
					.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
					.Where(m => !m.IsGenericMethod)
					.GroupBy(m => m.Name)
					.ToDictionary(
						p => p.Key,
						p => p.GroupBy(r => r.GetParameters().Length).ToDictionary(r => r.Key, r => r.ToList()));

			genInstanceMethods =
				objectType
					.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
					.Where(m => m.IsGenericMethod)
					.GroupBy(m => m.Name)
					.ToDictionary(
						p => p.Key,
						p => p.GroupBy(r => r.GetParameters().Length).ToDictionary(r => r.Key, r => r.ToList()));
		}

		/// <summary>
		/// Echtes internes Objekt
		/// </summary>
		public object Object => objectInstance;

        /// <summary>
		/// Neues exposed objekt erstellen
		/// </summary>
		public static dynamic New<T>(params object[] parameters)
		{
			return New(typeof(T), parameters);
		}

		/// <summary>
		/// Neues exposed objekt erstellen
		/// </summary>
		public static dynamic New(Type type, params object[] parameters)
		{
			return new ExposedObject(Create(type, parameters));
		}

		private static object Create(Type type, params object[] parameters)
		{
			ConstructorInfo constructorInfo = GetConstructorInfo(type, parameters);
			return constructorInfo.Invoke(parameters);
		}

		/// <summary>
		/// GetConstructorInfo
		/// </summary>
		public static ConstructorInfo GetConstructorInfo(Type type, params object[] parameters)
		{
			ConstructorInfo[] constructorInfo = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (constructorInfo.Any())
			{
				foreach (ConstructorInfo info in constructorInfo)
				{
					if (info.GetParameters().Count() == parameters.Count())
						return info;
				}
				return constructorInfo[0];
			}
			//throw new MissingMemberException(type.FullName, string.Format(".ctor({0})", string.Join(", ", Array.ConvertAll(args, t => t.FullName))));
			throw new MissingMemberException(type.FullName);
		}

		/// <summary>
		/// Exposed object aus einer objekt instanz erstellen
		/// </summary>
		public static dynamic From(object obj)
		{
			return new ExposedObject(obj);
		}

		/// <summary>
		/// Cast
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="t"></param>
		/// <returns></returns>
		public static T Cast<T>(ExposedObject t)
		{
			return (T)t.objectInstance;
		}

		/// <summary>
		/// Member Invoken
		/// </summary>
		/// <param name="binder"></param>
		/// <param name="args"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			// Get type args of the call
			Type[] typeArgs = ExposedObjectHelper.GetTypeArgs(binder);
			if (typeArgs != null && typeArgs.Length == 0) typeArgs = null;


			// Try to call a non-generic instance method
			if (typeArgs == null
					&& instanceMethods.ContainsKey(binder.Name)
					&& instanceMethods[binder.Name].ContainsKey(args.Length)
					&& ExposedObjectHelper.InvokeBestMethod(args, objectInstance, instanceMethods[binder.Name][args.Length], out result))
			{
				return true;
			}


			// Try to call a generic instance method
			if (instanceMethods.ContainsKey(binder.Name)
					&& instanceMethods[binder.Name].ContainsKey(args.Length))
			{
				var methods = (from method in genInstanceMethods[binder.Name][args.Length]
							   where method.GetGenericArguments().Length == typeArgs.Length
							   select method.MakeGenericMethod(typeArgs)).ToList();

				if (ExposedObjectHelper.InvokeBestMethod(args, objectInstance, methods, out result))
				{
					return true;
				}
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Member setzen
		/// </summary>
		/// <param name="binder"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			var propertyInfo = objectType.GetProperty(
				binder.Name,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (propertyInfo != null)
			{
				propertyInfo.SetValue(objectInstance, value, null);
				return true;
			}

			var fieldInfo = objectType.GetField(
				binder.Name,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (fieldInfo != null)
			{
				fieldInfo.SetValue(objectInstance, value);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Member finden
		/// </summary>
		/// <param name="binder"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			var propertyInfo = objectInstance.GetType().GetProperty(
				binder.Name,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (propertyInfo != null)
			{
				result = propertyInfo.GetValue(objectInstance, null);
				return true;
			}

			var fieldInfo = objectInstance.GetType().GetField(
				binder.Name,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (fieldInfo != null)
			{
				result = fieldInfo.GetValue(objectInstance);
				return true;
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Convert
		/// </summary>
		/// <param name="binder"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			result = objectInstance;
			return true;
		}
	}

	internal class ExposedObjectHelper
	{
		private static readonly Type CsharpInvokePropertyType =
			typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
				.Assembly
				.GetType("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");

		internal static bool InvokeBestMethod(object[] args, object target, List<MethodInfo> instanceMethods, out object result)
		{
			if (instanceMethods.Count == 1)
			{
				// Just one matching instance method - call it
				if (TryInvoke(instanceMethods[0], target, args, out result))
				{
					return true;
				}
			}
			else if (instanceMethods.Count > 1)
			{
				// Find a method with best matching parameters
				MethodInfo best = null;
				Type[] bestParams = null;
				Type[] actualParams = args.Select(p => p == null ? typeof(object) : p.GetType()).ToArray();

				Func<Type[], Type[], bool> isAssignableFrom = (a, b) => !a.Where((t, i) => !t.IsAssignableFrom(b[i])).Any();


				foreach (var method in instanceMethods.Where(m => m.GetParameters().Length == args.Length))
				{
					Type[] mParams = method.GetParameters().Select(x => x.ParameterType).ToArray();
					if (isAssignableFrom(mParams, actualParams))
					{
						if (best == null || isAssignableFrom(bestParams, mParams))
						{
							best = method;
							bestParams = mParams;
						}
					}
				}

				if (best != null && TryInvoke(best, target, args, out result))
				{
					return true;
				}
			}

			result = null;
			return false;
		}

		internal static bool TryInvoke(MethodInfo methodInfo, object target, object[] args, out object result)
		{
			try
			{
				result = methodInfo.Invoke(target, args);
				return true;
			}
			catch (TargetInvocationException) { }
			catch (TargetParameterCountException) { }

			result = null;
			return false;

		}

		internal static Type[] GetTypeArgs(InvokeMemberBinder binder)
		{
			if (CsharpInvokePropertyType.IsInstanceOfType(binder))
			{
				PropertyInfo typeArgsProperty = CsharpInvokePropertyType.GetProperty("TypeArguments");
				return ((IEnumerable<Type>)typeArgsProperty.GetValue(binder, null)).ToArray();
			}
			return null;
		}

	}


}