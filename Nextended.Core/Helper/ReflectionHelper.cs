using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Linq;
using Nextended.Core.Extensions;
using PropertyAttributes = System.Reflection.PropertyAttributes;

namespace Nextended.Core.Helper
{
	/// <summary>
	/// ReflectionHelper
	/// </summary>
	public static class ReflectionHelper
	{
		private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> interfaceTypeCache = new();
        private static readonly Dictionary<string, Type> _typeCache = new();

        public static T[] FindAllValuesOf<T>(object instance, ReflectReadSettings settings = null)
        {
            if (instance == null)
                return Array.Empty<T>();

            settings ??= ReflectReadSettings.AllPublic;
            var bindingFlags = settings.BindingFlags;
            var type = instance.GetType();

            Func<Type, bool> typeMatchFunc = settings.TypeMatch switch
            {
                ReflectTypeMatch.ExactType => (t) => t == type,
                ReflectTypeMatch.IsAssignableFrom => (t) => t.IsAssignableFrom(type),
                ReflectTypeMatch.IsAssignableTo => (t) => t.IsAssignableTo(type),
                _ => (t) => true
            };

            var loadFields = settings.MemberMethod.HasFlag(MemberMethod.GetFields);
            var lodProperties = settings.MemberMethod.HasFlag(MemberMethod.GetProperty);

            List<MemberInfo> members = new List<MemberInfo>();
            do
            {			                                
				if (loadFields)
					members.AddRange(type.GetFields(bindingFlags).Where(f => f.FieldType == typeof(T) && typeMatchFunc(f.DeclaringType)));
				if (lodProperties)
					members.AddRange(type.GetProperties(bindingFlags).Where(p => p.PropertyType == typeof(T) && typeMatchFunc(p.DeclaringType)));

                type = settings.TraverseHierarchy ? type.BaseType : null;

            } while (type != null);

            
            List<MemberInfo> distinctMembers = settings.MemberDistinct switch
            {
                MemberDistinct.Default => members.Distinct().ToList(),
                MemberDistinct.ByName => members.DistinctBy(n => n.Name).ToList(),
                _ => members
            };

            var values = distinctMembers.Select(member =>
            {
                return member switch
                {
                    FieldInfo field => (T)field.GetValue(instance),
                    PropertyInfo property => (T)property.GetValue(instance),
                    _ => default(T)
                };
            }).Where(value => value != null).ToArray();

            return values;
        }
		

        /// <summary>
        /// PublicBindingFlags
        /// </summary>
        public const BindingFlags PublicBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty;

        public static void ClearTypeCache()
        {
            _typeCache.Clear();
			interfaceTypeCache.Clear();
        }


        public static IStructuredDataObject CreateTypeAndDeserialize(string content, string typeName = "", bool cacheTypes = false)
        {
            if (!StructuredDataTypeValidator.TryDetectInputType(content, out StructuredDataType inputType))
                throw new ArgumentException("Cannot determine the input format. Please specify it explicitly.");

            return CreateTypeAndDeserialize(content, inputType, typeName, cacheTypes);
        }

        public static Type CreateTypeFor(string content, string typeName = "", bool cacheTypes = false)
        {
            if (!StructuredDataTypeValidator.TryDetectInputType(content, out StructuredDataType inputType))
                throw new ArgumentException("Cannot determine the input format. Please specify it explicitly.");

            return CreateTypeFor(content, inputType, typeName, cacheTypes);
        }


        public static IStructuredDataObject CreateTypeAndDeserialize(string content, StructuredDataType structuredDataType, string typeName = "", bool cacheTypes = false)
        {
            typeName = GenerateTypeName(typeName);
            var parsedObject = ParseContent(content, structuredDataType);
            var dynamicType = CreateTypeFromJObject(parsedObject, typeName, structuredDataType, cacheTypes);
            return parsedObject.ToObject(dynamicType) as IStructuredDataObject;
        }

        public static Type CreateTypeFor(string content, StructuredDataType structuredDataType, string typeName = "", bool cacheTypes = false)
        {
            typeName = GenerateTypeName(typeName);
            var parsedObject = ParseContent(content, structuredDataType);
            return CreateTypeFromJObject(parsedObject, typeName, structuredDataType, cacheTypes);
        }

        public static IStructuredDataObject CreateTypeAndDeserialize(IDictionary<string, object> contentDict, string typeName = "", bool cacheTypes = false)
        {
            typeName = GenerateTypeName(typeName);
            var jObject = JObject.FromObject(contentDict);
            var dynamicType = CreateTypeFromJObject(jObject, typeName, StructuredDataType.Json, cacheTypes);
            return jObject.ToObject(dynamicType) as IStructuredDataObject;
        }

        public static Type CreateTypeFor(IDictionary<string, object> contentDict, string typeName = "", bool cacheTypes = false)
        {
            typeName = GenerateTypeName(typeName);
            var jObject = JObject.FromObject(contentDict);
            return CreateTypeFromJObject(jObject, typeName, StructuredDataType.Json, cacheTypes);
        }


        private static string GenerateTypeName(string typeName) => !string.IsNullOrEmpty(typeName) ? typeName : $"DynamicType_{Guid.NewGuid().ToFormattedId().Replace("-","")}";

        private static IJObjectParser GetParser(StructuredDataType structuredDataType)
        {
            return structuredDataType switch
            {
                StructuredDataType.Json => new JsonJObjectParser(),
                StructuredDataType.Xml => new XmlJObjectParser(),
                StructuredDataType.Yaml => new YamlJObjectParser(),
                _ => throw new ArgumentException("Unsupported input type")
            };
        }

        private static JObject ParseContent(string content, StructuredDataType structuredDataType)
        {
            IJObjectParser ijObjectParser = GetParser(structuredDataType);
            return ijObjectParser.Parse(content);
        }


        private static Type CreateTypeFromJObject(JObject jObject, string typeName, StructuredDataType originalInputType, bool cacheTypes)
        {
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IStructuredDataObject));

            MethodInfo convertMethod = typeof(StructuredDataFormatConverter).GetMethod(nameof(StructuredDataFormatConverter.ConvertToString), new[] { typeof(object), typeof(StructuredDataType) });


            // For the method that takes an InputType
            var toStringMethod = typeBuilder.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), new[] { typeof(StructuredDataType) });
            var interfaceImplIl = toStringMethod.GetILGenerator();

            interfaceImplIl.Emit(OpCodes.Ldarg_0); // Load the current object onto the stack
            interfaceImplIl.Emit(OpCodes.Ldarg_1); // Load the StructuredDataType argument onto the stack
            interfaceImplIl.Emit(OpCodes.Call, convertMethod); // Call the method
            interfaceImplIl.Emit(OpCodes.Ret); // Return the result

            // For the default ToString() method
            var defaultToString = typeBuilder.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(string), Type.EmptyTypes);
            var toStringIl = defaultToString.GetILGenerator();

            toStringIl.Emit(OpCodes.Ldarg_0); // Load the current object
            toStringIl.Emit(OpCodes.Ldc_I4, (int)originalInputType); // Load the original StructuredDataType as an integer
            toStringIl.Emit(OpCodes.Call, toStringMethod); // Call the toStringMethod
            toStringIl.Emit(OpCodes.Ret); // Return the result


            foreach (var property in jObject)
            {
                var propertyType = GetTypeForToken(property.Value, typeBuilder, property.Key, originalInputType, cacheTypes);
                var fieldBuilder = typeBuilder.DefineField("_" + property.Key, propertyType, FieldAttributes.Private);
                var propertyBuilder = typeBuilder.DefineProperty(property.Key, PropertyAttributes.HasDefault, propertyType, null);

                var getMethodBuilder = typeBuilder.DefineMethod("get_" + property.Key, MethodAttributes.Public, propertyType, Type.EmptyTypes);
                var getIL = getMethodBuilder.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getIL.Emit(OpCodes.Ret);

                var setMethodBuilder = typeBuilder.DefineMethod("set_" + property.Key, MethodAttributes.Public, null, new[] { propertyType });
                var setIL = setMethodBuilder.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, fieldBuilder);
                setIL.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getMethodBuilder);
                propertyBuilder.SetSetMethod(setMethodBuilder);
            }

            return typeBuilder.CreateType();
        }

        private static Type GetTypeForToken(JToken token, TypeBuilder typeBuilder, string propertyName, StructuredDataType originalInputType, bool cacheTypes)
        {
            switch (token.Type)
            {
                case JTokenType.Integer: return typeof(int);
                case JTokenType.Float: return typeof(double);
                case JTokenType.String: return typeof(string);
                case JTokenType.Boolean: return typeof(bool);
                case JTokenType.Date: return typeof(DateTime);
                case JTokenType.Uri: return typeof(Uri);
                case JTokenType.Guid: return typeof(Guid);
                case JTokenType.TimeSpan: return typeof(TimeSpan);
                case JTokenType.Bytes: return typeof(byte[]);
                case JTokenType.Raw: return typeof(string);
                case JTokenType.Object:
                    var objectToken = token.Value<JObject>();
                    var structureKey = objectToken.ToString(Newtonsoft.Json.Formatting.None);

                    if (cacheTypes && _typeCache.TryGetValue(structureKey, out var forToken))
                        return forToken;

                    var newType = CreateTypeFromJObject(objectToken, $"{typeBuilder.Name}_{propertyName}", originalInputType, cacheTypes);
                    //var newType = CreateTypeFromJObject(objectToken, propertyName, originalInputType, cacheTypes);
                    if (cacheTypes)
                        _typeCache[structureKey] = newType;

                    return newType;

                case JTokenType.Array:
                    var array = token as JArray;
                    if (array.Count == 0)
                        return typeof(object[]);  // Empty array
                    var firstItem = array.First;
                    // Assuming all items have the same type
                    return GetTypeForToken(firstItem, typeBuilder, propertyName, originalInputType, cacheTypes).MakeArrayType();
                default: return typeof(object);
            }
        }

        /// <summary>
        /// Instanz für interface erzeugen
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <param name="coverUpAbstractMembers">Wenn true werden Abstrakte basis properties überdeckt</param>
        /// <returns></returns>
        public static TInterface CreateInstanceFromInterfaceOrAbstractType<TInterface>(bool coverUpAbstractMembers)
			where TInterface : class
		{
			return CreateInstanceFromInterfaceOrAbstractType(typeof(TInterface), coverUpAbstractMembers) as TInterface;
		}

		/// <summary>
		/// Instanz erzeugen
		/// </summary>
		public static T CreateInstance<T>() 
		{
			return CreateInstance<T>(true, false);
		}

		/// <summary>
		/// Instanz erzeugen
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="allowInterfacesAndAbstractClasses">if set to <c>true</c> [allow interfaces and abstract classes].</param>
		/// <param name="coverUpAbstractMembers">Wenn true werden Abstrakte basis properties überdeckt</param>
		/// <returns></returns>
		public static T CreateInstance<T>(bool allowInterfacesAndAbstractClasses,
			bool coverUpAbstractMembers) 
		{
			return (T)CreateInstance(typeof(T), allowInterfacesAndAbstractClasses, coverUpAbstractMembers);
		}

		/// <summary>
		/// Returns the default Value
		/// </summary>
		public static object GetDefaultValue(Type t)
		{
			if (t.IsValueType)
			{
				return Activator.CreateInstance(t);
			}
			return null;
		}

		/// <summary>
		/// PropertyInfo auch für parenttypes zurückgeben
		/// </summary>
		public static PropertyInfo[] GetPropertiesRecursive(this Type type, BindingFlags flags, Func<Type, bool> continueCondition = null)
		{
			if (continueCondition == null)
				continueCondition = t => t != typeof(object);

			var infos = new List<PropertyInfo>();
			Type typeToSearch = type;
			do
			{
				infos.AddRange(typeToSearch.GetProperties(flags));
				typeToSearch = typeToSearch.BaseType;
			} while (continueCondition(typeToSearch) && typeToSearch != null);
			return infos.ToArray();
		}

		/// <summary>
		/// PropertyInfo auch für parenttypes zurückgeben
		/// </summary>
		public static FieldInfo[] GetFieldsRecursive(this Type type, BindingFlags flags, Func<Type, bool> continueCondition = null)
		{
			continueCondition ??= t => t != typeof(object);

			var infos = new List<FieldInfo>();
			var typeToSearch = type;
			do
			{
				infos.AddRange(typeToSearch.GetFields(flags));
				typeToSearch = typeToSearch.BaseType;
			} while (continueCondition(typeToSearch) && typeToSearch != null);
			return infos.ToArray();
		}

		/// <summary>
		/// Eine instance erzeugen
		/// </summary>
		/// <param name="t">The t.</param>
		/// <param name="allowInterfacesAndAbstractClasses">Abstrakte klassen oder Interface instanzen erstellen</param>
		/// <param name="coverUpAbstractMembers">Wenn true werden Abstrakte basis properties überdeckt</param>
		/// <param name="tryResolve">Gibt an ob versucht werden soll per unity den typen aufzulösen</param>
		/// <param name="serviceProvider">Container</param>
		/// <returns></returns>
		public static object CreateInstance(Type t, bool allowInterfacesAndAbstractClasses,
			bool coverUpAbstractMembers, bool tryResolve, IServiceProvider serviceProvider = null)
		{
			object result = null;
			if (tryResolve)
			{
				try
				{
					result = (serviceProvider ?? new ServiceContainer()).GetService(t);
				}
				catch
				{
					result = null;
				}
			}
			return result ?? (CreateInstance(t, allowInterfacesAndAbstractClasses, coverUpAbstractMembers));
		}

		/// <summary>
		/// Eine instance erzeugen
		/// </summary>
		public static object CreateInstance(Type t)
		{
			return CreateInstance(t, true, false);
		}

        /// <summary>
        /// Eine instance erzeugen
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="allowInterfacesAndAbstractClasses">Abstrakte klassen oder Interface instanzen erstellen</param>
        /// <param name="coverUpAbstractMembers">Wenn true werden Abstrakte basis properties überdeckt</param>
        /// <returns></returns>
        public static object CreateInstance(Type t, bool allowInterfacesAndAbstractClasses, bool coverUpAbstractMembers, HashSet<Type> processedTypes = null)
        {
            processedTypes ??= new HashSet<Type>();

            // Detect recursion or cyclic dependency
            if (processedTypes.Contains(t))
            {
                throw new InvalidOperationException($"Cyclic dependency detected while creating an instance of type {t.FullName}");
            }

            // Mark type as processed
            processedTypes.Add(t);

            object result = null;

            try
            {
                if (allowInterfacesAndAbstractClasses && (t.IsInterface || t.IsArray || t.IsAbstract))
                {
                    result = CreateInstanceFromInterfaceOrAbstractType(t, coverUpAbstractMembers, processedTypes);
                }
                else if (t.GetConstructors().Any(info => !info.GetParameters().Any()))
                {
                    result = Activator.CreateInstance(t);
                }
            }
            catch (Exception ex)
            {
                // Log the error and try to fall back to default instance
                Debug.WriteLine($"Failed to create instance of type {t.FullName}: {ex.Message}");
                result = allowInterfacesAndAbstractClasses ? CreateInstance(t, false, false, processedTypes) : null;
            }

            // Fallback to the constructor with the fewest parameters
            if (result == null)
            {
                try
                {
                    var constructorInfo = t.GetConstructors().OrderBy(info => info.GetParameters().Count()).FirstOrDefault();
                    if (constructorInfo != null)
                    {
                        var parameters = constructorInfo.GetParameters()
                                                        .Select(param => CreateInstance(param.ParameterType, true, false, processedTypes))
                                                        .ToArray();
                        result = constructorInfo.Invoke(parameters);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to invoke constructor for type {t.FullName}: {ex.Message}");
                }
            }

            return result;
        }


		/// <summary>
		/// Findet den typen, der das angegebene interface implementiert
		/// </summary>
		public static Type FindImplementingType(Type interfaceType)
		{
            IEnumerable<Type> additionalTypesToCheck = new List<Type>
            {
                typeof(List<>),
                typeof(Collection<>),
                typeof(HashSet<>),
                typeof(Dictionary<,>),
                typeof(Comparer<>),
                typeof(Queue<>),
                typeof(Stack<>),
            };

			var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            Func<Type, IEnumerable<Type>> func = it => (from assembly in assemblies
														where assembly.GetName().Version > new System.Version(0, 0, 0, 0)
														from referencedAssembly in assembly.GetReferencedAssemblies()
														where assembly == it.Assembly || referencedAssembly.FullName == it.Assembly.GetName().FullName
														select assembly).SelectMany(assembly => assembly.GetTypes()).Concat(additionalTypesToCheck)
				.Where(type => type.ImplementsInterface(it))
				.ToList();

			IEnumerable<Type> implementingTypes = interfaceTypeCache.GetOrAdd(interfaceType, func).ToList();
			var result = implementingTypes.FirstOrDefault(
				type => type.Name.Equals(interfaceType.Name.Substring(1), StringComparison.InvariantCultureIgnoreCase))
				   ?? implementingTypes.FirstOrDefault();

            result = TryMakeGenericIfNeeded(interfaceType, result);
            return result;
        }

        private static Type TryMakeGenericIfNeeded(Type interfaceType, Type result)
        {
            try
            {
                if (interfaceType.IsGenericType && interfaceType.GenericTypeArguments.Any() && result != null &&
                    result.IsGenericType)
                {
					// TODO: Check if Generic param constraints are matching interfaceType.GenericTypeArguments
                    result = result.MakeGenericType(interfaceType.GenericTypeArguments);
                }
            } 
            catch
            {}

            return result;
        }

        /// <summary>
		/// Instanz für interface erzeugen
		/// </summary>
		/// <param name="interfaceType">Typ</param>
		/// <param name="coverUpAbstractMembers">Wenn true werden Abstrakte basis properties überdeckt</param>
		public static object CreateInstanceFromInterfaceOrAbstractType(Type interfaceType, bool coverUpAbstractMembers, HashSet<Type> processedTypes = null)
        {
            processedTypes ??= new HashSet<Type>();

            // Prevent recursion by tracking processed types
            if (processedTypes.Contains(interfaceType))
            {
                throw new InvalidOperationException($"Cyclic dependency detected while creating an instance for interface/abstract type {interfaceType.FullName}");
            }

            // Add type to the processed list to avoid recursion
            processedTypes.Add(interfaceType);

            // If the type is an IEnumerable, create an appropriate list or dictionary
            if (typeof(IEnumerable).IsAssignableFrom(interfaceType))
            {
                return CreateList(interfaceType);
            }

            // Only interfaces or abstract classes are allowed
            if (!interfaceType.IsInterface && !interfaceType.IsAbstract)
            {
                throw new NotSupportedException("Only interfaces or abstract types are supported.");
            }

            // Try to find an implementing type
            var existingType = FindImplementingType(interfaceType);
            if (existingType != null)
            {
                return CreateInstance(existingType, true, coverUpAbstractMembers, processedTypes);
            }

            // Dynamically create a type for the interface/abstract class
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
            var typeBuilder = CreateTypeBuilder(moduleBuilder, $"DynamicType_{interfaceType.Name}", interfaceType);

            // Add interface or abstract members as needed
            foreach (var property in interfaceType.GetProperties(PublicBindingFlags))
            {
                BuildProperty(typeBuilder, property.Name, property.PropertyType);
            }
            foreach (var method in interfaceType.GetMethods(PublicBindingFlags))
            {
                BuildEmptyMethod(typeBuilder, method.Name, method.ReturnType);
            }

            // Create the type and instantiate it
            var dynamicType = typeBuilder.CreateTypeInfo().AsType();
            return Activator.CreateInstance(dynamicType);
        }

        /// <summary>
        /// Enthaltene typen zurückgeben
        /// </summary>
        public static Type[] GetDeclaringTypes(Type t)
		{
			if (t.IsGenericType)
				return t.GetGenericArguments();
			return t.IsArray ? new[] { t.GetElementType() } : new[] { typeof(object) };
        }

		private static object CreateList(Type interfaceType)
		{
			Type typeToCreate = typeof(ArrayList);
			if (interfaceType.IsGenericType || interfaceType.IsArray)
			{
				typeToCreate = typeof(List<>);
				if (typeof(IDictionary).IsAssignableFrom(interfaceType) ||
					(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
					(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
				{
					typeToCreate = typeof(Dictionary<,>);
				}
                typeToCreate = typeToCreate.MakeGenericType(GetDeclaringTypes(interfaceType));
			}
			return Activator.CreateInstance(typeToCreate);
		}

		#region Private Helper for typebuilder

		private static void BuildEmptyMethod(TypeBuilder typeBuilder, string name, Type type)
		{
			const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
												MethodAttributes.Virtual;

			MethodBuilder getter = typeBuilder.DefineMethod(name, getSetAttr, type, Type.EmptyTypes);

			ILGenerator getIL = getter.GetILGenerator();
			getIL.Emit(OpCodes.Ldarg_0);
			getIL.Emit(OpCodes.Ret);
		}

		private static void BuildField(TypeBuilder typeBuilder, string name, Type type)
		{
			typeBuilder.DefineField(name, type, FieldAttributes.Public);
		}

		private static void BuildProperty(TypeBuilder typeBuilder, string name, Type type)
		{
			FieldBuilder field = typeBuilder.DefineField("m" + name, type, FieldAttributes.Private);
			PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, type, null);

			const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
												MethodAttributes.Virtual;

			MethodBuilder getter = typeBuilder.DefineMethod("get_" + name, getSetAttr, type, Type.EmptyTypes);

			ILGenerator getIL = getter.GetILGenerator();
			getIL.Emit(OpCodes.Ldarg_0);
			getIL.Emit(OpCodes.Ldfld, field);
			getIL.Emit(OpCodes.Ret);

			MethodBuilder setter = typeBuilder.DefineMethod("set_" + name, getSetAttr, null, new[] { type });

			ILGenerator setIL = setter.GetILGenerator();
			setIL.Emit(OpCodes.Ldarg_0);
			setIL.Emit(OpCodes.Ldarg_1);
			setIL.Emit(OpCodes.Stfld, field);
			setIL.Emit(OpCodes.Ret);


			propertyBuilder.SetGetMethod(getter);
			propertyBuilder.SetSetMethod(setter);
		}


		private static TypeBuilder CreateTypeBuilder(ModuleBuilder modBuilder, string typeName, Type interfaceType)
		{
			Type parentType = typeof(object);
			if (!interfaceType.IsInterface)
				parentType = interfaceType;
			TypeBuilder typeBuilder = modBuilder.DefineType(typeName,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass |
				TypeAttributes.BeforeFieldInit |
				TypeAttributes.AutoLayout,
				parentType, interfaceType.IsInterface ? new[] { interfaceType } : new Type[0]);

			return typeBuilder;
		}


		#endregion


		/// <summary>
		/// Simple Implicit or Explicit cast
		/// </summary>
		public static T SimpleCast<T>(object o)
		{
			var res = SimpleCast(o, typeof(T));
			if (res != null)
				return (T)res;
			return default(T);
		}

		/// <summary>
		/// Simple Implicit or Explicit cast
		/// </summary>
		/// <param name="o"></param>
		/// <param name="targetType"></param>
		/// <returns></returns>
		public static object SimpleCast(object o, Type targetType)
		{
			object result;
			TrySimpleCast(o, targetType, out result);
			return result;
		}

		/// <summary>
		/// Simple Implicit or Explicit cast
		/// </summary>
		public static bool TrySimpleCast(object o, Type targetType, out object result)
		{
			result = null;
			MethodInfo mi = o.GetType().GetMethods((BindingFlags.Public | BindingFlags.Static)).
				FirstOrDefault(info => (info.Name == "op_Implicit" || info.Name == "op_Explicit")
										&& targetType.IsAssignableFrom(info.ReturnType) && info.GetParameters().Count() == 1
										&& o.GetType().IsAssignableFrom(info.GetParameters()[0].ParameterType))
							??
							targetType.GetMethods((BindingFlags.Public | BindingFlags.Static)).
							FirstOrDefault(info => (info.Name == "op_Implicit" || info.Name == "op_Explicit")
													&& targetType.IsAssignableFrom(info.ReturnType) && info.GetParameters().Count() == 1
													&& o.GetType().IsAssignableFrom(info.GetParameters()[0].ParameterType));

			if (mi != null)
			{
				var invoke = mi.Invoke(null, new[] { o });
				result = invoke;
				return true;
			}

			if (targetType.IsInstanceOfType(o))
			{
				var typeConverter = TypeDescriptor.GetConverter(targetType);
				if (typeConverter.CanConvertTo(targetType))
				{
					result = typeConverter.ConvertTo(o, targetType);
					return true;
				}
				typeConverter = TypeDescriptor.GetConverter(o.GetType());
				if (typeConverter.CanConvertFrom(o.GetType()))
				{
					result = typeConverter.ConvertFrom(o);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Aus einem Typen ein Dictionary erzeugen
		/// </summary>
		public static IDictionary<string, object> ToDictionary(this Type type, Func<Type, MemberInfo, bool> condition = null)
		{
			var flags = PublicBindingFlags | BindingFlags.Static;

			condition ??= (t, f) => true;
			var result = new Dictionary<string, object>();

			result.AddRange(type.GetFields(flags)
				.Where(fi => condition(fi.FieldType, fi))
				.ToDictionary(fi => fi.Name, fi => fi.GetValue(null)));

			result.AddRange(type.GetProperties(flags)
				.Where(fi => condition(fi.PropertyType, fi))
				.ToDictionary(fi => fi.Name, fi => fi.GetValue(null)));

			return result;
		}

		/// <summary>
		/// Gibt alle Properties mit wert zurück
		/// </summary>
		/// <param name="objectValue">Objekt</param>
		/// <param name="except">Alle Properties ausser diesen</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		public static Dictionary<string, object> GetProperties(object objectValue, params string[] except)
		{
			var result = new Dictionary<string, object>();
			BindingFlags bindingInfo = BindingFlags.Public | BindingFlags.Static;
			Type objectType = objectValue as Type;
			object objectInstance = null;
			if (objectType == null)
			{
				bindingInfo = BindingFlags.Public | BindingFlags.Instance;
				objectType = objectValue.GetType();
				objectInstance = objectValue;
			}

			foreach (var propertyInfo in from info in objectType.GetProperties(bindingInfo)
										 where !except.Contains(info.Name)
										 select info)
			{
				try
				{
					var value = propertyInfo.GetValue(objectInstance, new object[0]);
					result.Add(propertyInfo.Name, value);
				}
				catch { }
			}
			return result;
		}

		/// <summary>
		/// Gibt die signatur der methode zurück
		/// </summary>
		public static string GetSignature(this MethodInfo method, bool callable = false)
		{
			var firstParam = true;
			var sigBuilder = new StringBuilder();
			if (!callable)
			{
				if (method.IsPublic)
					sigBuilder.Append("public ");
				else if (method.IsPrivate)
					sigBuilder.Append("private ");
				else if (method.IsAssembly)
					sigBuilder.Append("internal ");
				if (method.IsFamily)
					sigBuilder.Append("protected ");
				if (method.IsStatic)
					sigBuilder.Append("static ");
				sigBuilder.Append(TypeName(method.ReturnType));
				sigBuilder.Append(' ');
			}
			sigBuilder.Append(method.Name);

			// Add method generics
			if (method.IsGenericMethod)
			{
				sigBuilder.Append("<");
				foreach (var g in method.GetGenericArguments())
				{
					if (firstParam)
						firstParam = false;
					else
						sigBuilder.Append(", ");
					sigBuilder.Append(TypeName(g));
				}
				sigBuilder.Append(">");
			}
			sigBuilder.Append("(");
			firstParam = true;
			var secondParam = false;
			foreach (var param in method.GetParameters())
			{
				if (firstParam)
				{
					firstParam = false;
					if (method.IsDefined(typeof(ExtensionAttribute), false))
					{
						if (callable)
						{
							secondParam = true;
							continue;
						}
						sigBuilder.Append("this ");
					}
				}
				else if (secondParam)
					secondParam = false;
				else
					sigBuilder.Append(", ");
				if (param.ParameterType.IsByRef)
					sigBuilder.Append("ref ");
				else if (param.IsOut)
					sigBuilder.Append("out ");
				if (!callable)
				{
					sigBuilder.Append(TypeName(param.ParameterType));
					sigBuilder.Append(' ');
				}
				sigBuilder.Append(param.Name);
			}
			sigBuilder.Append(")");
			return sigBuilder.ToString();
		}

		/// <summary>
		/// Prüft ob ein bestimmter Typ ein bestimmtes interface implementiert
		/// </summary>
		public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            type = TryMakeGenericIfNeeded(interfaceType, type);
            Type[] array = type.FindInterfaces((typeObj, criteriaObj) => typeObj == (Type)criteriaObj, interfaceType);
			return array.Length > 0; // || interfaceType.IsAssignableFrom(type);
		}

		/// <summary>
		/// Get full type name with full namespace names
		/// </summary>
		/// <param name="type">Type. May be generic or nullable</param>
		/// <returns>Full type name, fully qualified namespaces</returns>
		public static string TypeName(Type type)
		{
			var nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
				return nullableType.Name + "?";

			if (!type.IsGenericType)
				switch (type.Name)
				{
					case "String": return "string";
					case "Int32": return "int";
					case "Decimal": return "decimal";
					case "Object": return "object";
					case "Void": return "void";
					default:
						{
							return String.IsNullOrWhiteSpace(type.FullName) ? type.Name : type.FullName;
						}
				}

			var sb = new StringBuilder(type.Name.Substring(0,
			type.Name.IndexOf('`'))
			);
			sb.Append('<');
			var first = true;
			foreach (var t in type.GetGenericArguments())
			{
				if (!first)
					sb.Append(',');
				sb.Append(TypeName(t));
				first = false;
			}
			sb.Append('>');
			return sb.ToString();
		}



		/// <summary>
		/// Gibt den wert einer Property eines Objektes zurück
		/// </summary>
		public static object GetValue(object obj, string property)
		{
			var dataRowView = obj as DataRowView;
			if (dataRowView != null)
			{
				return dataRowView[property];
			}
			if (obj == null)
				return null;
			if (String.IsNullOrEmpty(property))
				return obj;
			var propertyInfo = obj.GetType().GetProperty(property);
			if (propertyInfo == null)
				return obj;
			var result = propertyInfo.GetValue(obj, new object[] { });
			return result ?? obj;
		}

		/// <summary>
		/// Gibt die Methode zurück, von der der Aufruf der Methode, die "GetCallingMethod" aufgerufen hat kam
		/// </summary>
		public static MethodBase GetCallingMethod(int skip = 0)
		{			
			var st = new StackTrace(2 + skip, true);

			try
			{
				StackFrame sf = st.GetFrame(0);
				if (sf != null)
					return sf.GetMethod();
			}
			catch (Exception)
			{
				return MethodBase.GetCurrentMethod();
			}

			return MethodBase.GetCurrentMethod();
		}
    }
	
	public class ReflectReadSettings
	{
		public BindingFlags BindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        public ReflectTypeMatch TypeMatch { get; set; }
        public bool TraverseHierarchy { get; set; }
        public MemberDistinct MemberDistinct { get; set; } = MemberDistinct.Default;
        public MemberMethod MemberMethod { get; set; } = MemberMethod.All;

        public static ReflectReadSettings Default => new();
        public static ReflectReadSettings AllPublic => new() { BindingFlags = BindingFlags.Public | BindingFlags.Instance };
        public static ReflectReadSettings AllPublicExactType => AllPublic.SetProperties(v => v.TypeMatch = ReflectTypeMatch.ExactType);
        public static ReflectReadSettings AllPublicIsAssignableTo => AllPublic.SetProperties(v => v.TypeMatch = ReflectTypeMatch.IsAssignableTo);
        public static ReflectReadSettings AllPublicIsAssignableFrom => AllPublic.SetProperties(v => v.TypeMatch = ReflectTypeMatch.IsAssignableFrom);

        public static ReflectReadSettings All => new() { BindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic };
        public static ReflectReadSettings AllExactType => All.SetProperties(v => v.TypeMatch = ReflectTypeMatch.ExactType);
        public static ReflectReadSettings AllIsAssignableTo => All.SetProperties(v => v.TypeMatch = ReflectTypeMatch.IsAssignableTo);
        public static ReflectReadSettings AllIsAssignableFrom => All.SetProperties(v => v.TypeMatch = ReflectTypeMatch.IsAssignableFrom);
        public static ReflectReadSettings AllWithHierarchyTraversal => All.SetProperties(s => s.TraverseHierarchy = true);
    }

	[Flags]
    public enum MemberMethod
    {
		All = 3,
        GetFields = 1,
        GetProperty = 2,        
    }

    public enum MemberDistinct
	{ 
		Default,
		None,
		ByName
	}


    public enum ReflectTypeMatch
	{
		NoCheck,
		ExactType,
		IsAssignableTo,
		IsAssignableFrom
	}
}