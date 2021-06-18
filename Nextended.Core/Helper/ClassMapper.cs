using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper
{
	/// <summary>
	/// Mapping von Klassen
	/// </summary>
	public class ClassMapper : IDisposable
	{
		private readonly object lockObject = new object();
		private List<TypeConverter> converterList;

        internal static ClassMapper defaultClassMapperInstance;

		/// <summary>
		/// Settings für das Mappingverhalten
		/// </summary>
		private ClassMappingSettings classMappingSettings;


		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Object"/> class.
		/// </summary>
		public ClassMapper(ClassMappingSettings settings)
		{
			classMappingSettings = settings;
		}

		/// <summary>
		/// Initializes a new instance of the classmapper class.
		/// </summary>
		public ClassMapper()
		{ }


		/// <summary>
		/// Einstellungen für das Mappingverhalten setzen
		/// </summary>
		public ClassMapper SetSettings(ClassMappingSettings settings)
		{
			classMappingSettings = settings;
			return this;
		}

		/// <summary>
		/// Einstellungen für das Mappingverhalten setzen
		/// </summary>
		public ClassMapper SetSettings(params Action<ClassMappingSettings>[] o)
		{
			classMappingSettings ??= ClassMappingSettings.Default;
			o.Apply(action => action(classMappingSettings));
			return this;
		}

        public ClassMapper SetAsDefaultMapper()
        {
            defaultClassMapperInstance = this;
            return this;
        }

		/// <summary>
		/// Mapped eine Klasse auf eine andere
		/// </summary>
		/// <param name="input">Die Instanz der eingangsklasse</param>
		/// <param name="tResult">Ergebnistyp</param>
		public object Map<TInput>(TInput input, Type tResult)
		{
			classMappingSettings ??= ClassMappingSettings.Default;

			converterList ??= new List<TypeConverter>(classMappingSettings.TypeConverters);

			if (!classMappingSettings.IgnoreGlobalConverters)
				converterList.AddRange(ClassMappingSettings.GlobalConverters);

			#region Direkt Konvertieren ohne Properties zu setzen

			if (tResult.IsValueType || tResult.IsNullableType() || tResult == typeof(string) ||
				input.GetType().IsValueType || input is string ||
				(input is IEnumerable && typeof(IEnumerable).IsAssignableFrom(tResult)))
			{
				// Direkt Konvertieren ohne Properties zu setzen
				try
				{
					var res = GetConvertedValue(input, tResult);
					return res;
				}
				catch
				{
					if (!classMappingSettings.IgnoreExceptions)
						throw;
				}
			}

			#endregion

			#region Dictionary zu Objekt

			if (input is IEnumerable && !typeof(IEnumerable).IsAssignableFrom(tResult))
			{
				var res = CreateInstance(tResult);
				foreach (var o in (IEnumerable)input)
				{
					if (o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
					{
						var key = o.GetType().GetProperty("Key").GetValue(o);
						var value = o.GetType().GetProperty("Value").GetValue(o);
						SetValue(key.ToString(), res, value);
					}
				}
				return res;
			}

			#endregion

			object result;
			if (ReflectionHelper.TrySimpleCast(input, tResult, out result))
				return result;

			result = DoMap(input, CreateInstance(tResult));

			return result;
		}

		/// <summary>
		/// Mapped eine Klasse auf eine andere
		/// </summary>
		/// <typeparam name="TInput">Typ der eingansgsklasse.</typeparam>
		/// <typeparam name="TResult">Ergebnistyp</typeparam>
		/// <param name="input">Die Instanz der eingangsklasse</param>
		/// <param name="differentMappingAssignments">Mappings die nicht automatisch erkannt werden können, können als ausdruckszuweisung angegeben werden z.B (object2, object1) => object2.Gender = object1.IsMale</param>
		/// <example>
		///  Object2 o2 = MapTo[Object1, Object2]((object2, object1) => object2.Gender = object1.IsMale);
		/// </example>
		public TResult Map<TInput, TResult>(TInput input,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{

			TResult result = (TResult)Map(input, typeof(TResult));
			ExecuteCustomAssignment(input, differentMappingAssignments, result);
			return result;
		}

		/// <summary>
		/// Mapped eine Klasse auf eine andere
		/// </summary>
		public Task<object> MapAsync<TInput>(TInput input, Type tResult)
		{
			return Task.Factory.StartNew(() => Map(input, tResult));
		}


		/// <summary>
		/// Mapped eine Klasse auf eine andere
		/// </summary>
		/// <typeparam name="TInput">Typ der eingansgsklasse.</typeparam>
		/// <typeparam name="TResult">Ergebnistyp</typeparam>
		/// <param name="input">Die Instanz der eingangsklasse</param>
		/// <param name="differentMappingAssignments">Mappings die nicht automatisch erkannt werden können, können als ausdruckszuweisung angegeben werden z.B (object2, object1) => object2.Gender = object1.IsMale</param>
		/// <example>
		///  Object2 o2 = MapTo[Object1, Object2]((object2, object1) => object2.Gender = object1.IsMale);
		/// </example>
		public Task<TResult> MapAsync<TInput, TResult>(TInput input,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return Task.Factory.StartNew(() => Map(input, differentMappingAssignments));
		}

		private static void ExecuteCustomAssignment<TInput, TResult>(TInput input, Action<TResult, TInput>[] differentMappingAssignments, TResult result)
		{
			if (differentMappingAssignments != null)
			{
				foreach (Action<TResult, TInput> action in differentMappingAssignments)
					action(result, input);
			}
		}

		private BindingFlags GetBindingFlags()
		{
			if (classMappingSettings != null && classMappingSettings.IncludePrivateFields)
				return ReflectionHelper.PublicBindingFlags | BindingFlags.NonPublic;
			return ReflectionHelper.PublicBindingFlags;
		}

		private object CreateInstance(Type t)
		{
			return ReflectionHelper.CreateInstance(t, true,
				classMappingSettings.CoverUpAbstractMembers,
				classMappingSettings.TryContainerResolve,
				classMappingSettings.ServiceProvider);
		}

		private object DoMapAsync(object input, object result)
		{
			var propertyInfos = input.GetType().GetPropertiesRecursive(GetBindingFlags()).Cast<MemberInfo>().ToList();
			propertyInfos.AddRange(input.GetType().GetFields(GetBindingFlags()));
			var tasks = propertyInfos.Select(propertyInfo => propertyInfo).Select(prop => Task.Factory.StartNew(() => TrySetProp(input, result, prop))).ToList();
			Task.WaitAll(tasks.ToArray());
			return result;
		}


		private object DoMap(object input, object result)
		{
			if (classMappingSettings.ShouldEnumeratePropertiesAsync)
				return DoMapAsync(input, result);

			var propertyInfos = input.GetType().GetPropertiesRecursive(GetBindingFlags()).Cast<MemberInfo>().ToList();
			propertyInfos.AddRange(input.GetType().GetFields(GetBindingFlags()));

			foreach (var prop in propertyInfos)
			{
				TrySetProp(input, result, prop);
			}

			return result;
		}

		private void TrySetProp(object input, object result, MemberInfo prop)
		{
			string propName = prop.Name;
			if (!classMappingSettings.PropertiesToIgnore.Contains(prop))
			{
				object propValue = GetPropertyOrFieldValue(propName, input);
				if (propValue != null)
					SetValue(propName, result, propValue);
			}
			else if (classMappingSettings.HasAssignments)
			{
				foreach (KeyValuePair<MemberInfo, MemberInfo> assign in (classMappingSettings.PropertiesToAssign.Where(pair => pair.Key == prop)))
				{
					object propValue = GetPropertyOrFieldValue(propName, input);
					if (propValue != null)
						SetValue(assign.Value.Name, result, propValue);
				}
			}
		}

		private object TryAllExistingConverters(Type targetType, object currentValue)
		{
			if (converterList == null)
				return null;
			return (from typeConverter in converterList
					where typeConverter.CanConvertFrom(currentValue.GetType())
						&& typeConverter.CanConvertTo(targetType)
					select typeConverter.ConvertTo(currentValue, targetType)).FirstOrDefault(result => result != null);
		}

		private bool ShouldEnumerateAsync(object enumerable)
		{
			if (classMappingSettings.ShouldEnumerateListsAsync)
			{
				var list = enumerable as IList;
				if (list != null && list.Count > 100)
					return true;
				try
				{
					PropertyInfo countProp = enumerable.GetType().GetProperties().FirstOrDefault(info => info.Name == "Count");
					if (countProp != null)
					{
						if ((int)countProp.GetValue(enumerable) >= classMappingSettings.MinListCountToEnumerateAsync)
							return true;
					}
					MethodInfo countMethod = enumerable.GetType().GetMethods(ReflectionHelper.PublicBindingFlags).FirstOrDefault(info => info.Name == "Count");
					if (countMethod != null)
					{
						if ((int)countMethod.Invoke(enumerable, null) >= classMappingSettings.MinListCountToEnumerateAsync)
							return true;
					}
				}
				catch (Exception)
				{
					return false;
				}
			}
			return false;
		}

		internal static bool IsJson(string input)
		{
			input = input.Trim();
			return input.StartsWith("{") && input.EndsWith("}")
				   || input.StartsWith("[") && input.EndsWith("]");
		}

		private object GetConvertedValue(object currentValue, Type targetType)
		{
			if (currentValue != null)
			{
				Type currentValueType = currentValue.GetType();
				if (targetType != currentValueType)
				{
					// Wenn die Option dafür an ist und der ateulle wert einem default wert des valuetypes entspricht null zurückgeben
					if (!targetType.IsValueType && currentValueType.IsValueType
						&& classMappingSettings.DefaultValueTypeValuesAsNullForNonValueTypes
						&& ReflectionHelper.GetDefaultValue(currentValueType).Equals(currentValue))
					{
						return null;
					}

					object result;
					if (ReflectionHelper.TrySimpleCast(currentValue, targetType, out result))
						return result;

					result = TryAllExistingConverters(targetType, currentValue);
					if (result != null)
						return result;

					#region Behandlung für JSON

					if (classMappingSettings.ObjectToStringWithJSON && targetType == typeof(string) && currentValueType.IsClass)
					{
						return ToJson(currentValue);
					}

					if (classMappingSettings.CanConvertFromJSON && currentValueType == typeof(string) && targetType.IsClass && IsJson((string)currentValue))
					{
						return FromJson((string)currentValue, targetType);
					}

					#endregion


					#region Behandlung für IEnumerables und Dictionaries

					if (typeof(IEnumerable).IsAssignableFrom(targetType) && currentValue is IEnumerable)
					{
						Type[] targetValueTypesForEnumerable = ReflectionHelper.GetDeclaringTypes(targetType);

						//object resultListOrDictionary = ReflectionHelper.CreateInstanceFromInterface(targetType);
						object resultListOrDictionary = targetType.IsInterface || targetType.IsArray
							? ReflectionHelper.CreateInstanceFromInterfaceOrAbstractType(targetType, classMappingSettings.CoverUpAbstractMembers)
							: ReflectionHelper.CreateInstance(targetType, true, classMappingSettings.CoverUpAbstractMembers, classMappingSettings.TryContainerResolve);

						var resultList = resultListOrDictionary as IList;
						IDictionary resultDictionary = resultListOrDictionary as IDictionary;

						#region IEnumerable

						if (resultList != null)
						{
							if (ShouldEnumerateAsync(currentValue))
							{
								FillResultListAsync((IEnumerable)currentValue, targetValueTypesForEnumerable, resultList);
							}
							else
							{
								foreach (var value in (IEnumerable)currentValue)
									resultList.Add(GetConvertedValue(value, targetValueTypesForEnumerable[0]));
							}

							if (targetType.IsArray)
							{
								var arrayResult = Activator.CreateInstance(targetType, resultList.Count) as Array;
								if (arrayResult != null)
								{
									for (int index = 0; index < resultList.Count; index++)
									{
										arrayResult.SetValue(resultList[index], index);
									}
								}
								return arrayResult;
							}

							return resultList;
						}

						#endregion

						#region Dictionary

						if (resultDictionary != null)
						{
							foreach (object o in (IEnumerable)currentValue)
							{
								var key = o.GetType().GetProperty("Key").GetValue(o);
								var value = o.GetType().GetProperty("Value").GetValue(o);

								var v1 = GetConvertedValue(key, targetValueTypesForEnumerable[0]);
								var v2 = GetConvertedValue(value, targetValueTypesForEnumerable[1]);
								resultDictionary.Add(v1, v2);
							}
							return resultDictionary;
						}

						#endregion

						#region Fallback wenn keine IList ist per reflection eventuell add ausführen

						if (resultListOrDictionary != null)
						{
							try
							{
								var method = resultListOrDictionary.GetType().GetMethods(ReflectionHelper.PublicBindingFlags).FirstOrDefault(info => info.Name == "Add");
								if (method != null)
								{
									foreach (var value in (IEnumerable)currentValue)
										method.Invoke(resultListOrDictionary, new[] { GetConvertedValue(value, targetValueTypesForEnumerable[0]) });
									return resultListOrDictionary;
								}
							}
							catch { }
						}

						#endregion

					}

					if (currentValue is IEnumerable<char> && targetType == typeof(string))
						return new string(((IEnumerable<char>)currentValue).ToArray());

					#endregion

					#region Behandlung für BaseId's

					if (targetType.IsBaseId() && targetType.BaseType != null)
					{
						if (currentValue is Guid && ((Guid)currentValue) == Guid.Empty)
							return null;
						try
						{
							return Activator.CreateInstance(targetType, new[] { currentValue.MapTo(targetType.GetBaseIdBaseType().GetGenericArguments()[1], classMappingSettings) });
							//return Activator.CreateInstance(targetType, new[] { Convert.ChangeType(currentValue, targetType.BaseType.GetGenericArguments()[1]) });
						}
						catch (Exception)
						{
							if (!classMappingSettings.IgnoreExceptions)
								throw;
						}
					}
					if (currentValueType.IsBaseId())
					{
						currentValue = currentValue.GetType().GetProperty("Id").GetValue(currentValue, null);
						result = ReflectionHelper.SimpleCast(currentValue, targetType);
						if (result != null)
							return result;
					}

					#endregion

					if (currentValueType == typeof(string) && targetType.IsEnum)
					{
						var name = currentValue.ToString();
						MemberInfo memberInfo = targetType.GetMembers().FirstOrDefault(info => info.GetAttributes<XmlEnumAttribute>(false).Any(attribute => attribute.Name.Equals(currentValue.ToString(), classMappingSettings.MatchCaseForEnumNameConversion ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)));
						if (memberInfo != null)
						{
							name = memberInfo.Name;
						}
						return Enum.Parse(targetType, name, !classMappingSettings.MatchCaseForEnumNameConversion);
					}

					if (currentValueType.IsEnum && targetType == typeof(string))
					{
						MemberInfo[] memberInfos = currentValueType.GetMember(currentValue.ToString());
						if (memberInfos.Any())
						{
							var attribute = memberInfos[0].GetAttributes<XmlEnumAttribute>(false).FirstOrDefault();
							return attribute == null ? currentValue.ToString() : attribute.Name;
						}
					}

					TypeConverter typeConverter = new TypeConverter();
					if (typeConverter.CanConvertTo(targetType))
						return typeConverter.ConvertTo(currentValue, targetType);

					if (!targetType.IsValueType)
					{
						result = DoMap(currentValue, CreateInstance(targetType));
						if (result != null)
							return result;
					}

					if (classMappingSettings.AllowGuidConversion)
					{
						object convertedValue;
						if (TryGuidConversion(currentValue, targetType, out convertedValue))
							return convertedValue;
					}

					#region Std Fallback

					object parseRes;
					int intResult;
					uint uIntResult;
					double doubleResult;
					bool boolResult;
					DateTime dateTimeResult;
					Guid guidResult;
					Uri uriResult;

					var targetValueType = targetType.IsNullableType()
										? Nullable.GetUnderlyingType(targetType) : targetType;

					if (currentValue.GetType().IsNullableType() ||
						((targetType.IsNullableType() || targetType.IsValueType)))
					{
						var valueProp = currentValue.GetType().GetProperties(ReflectionHelper.PublicBindingFlags).FirstOrDefault(info => info.Name == "Value");
						if (valueProp != null)
							currentValue = valueProp.GetValue(currentValue);
					}

					if (targetValueType == typeof(int) && int.TryParse(currentValue.ToString(), out intResult))
						currentValue = intResult;
					if (targetValueType == typeof(uint) && uint.TryParse(currentValue.ToString(), out uIntResult))
						currentValue = uIntResult;
					else if (targetValueType == typeof(Guid) && Guid.TryParse(currentValue.ToString(), out guidResult))
						currentValue = guidResult;
					else if (targetValueType == typeof(DateTime) && DateTime.TryParse(currentValue.ToString(), out dateTimeResult))
						currentValue = dateTimeResult;
					else if (targetValueType == typeof(double) && double.TryParse(currentValue.ToString(), out doubleResult))
						currentValue = doubleResult;
					else if (targetValueType == typeof(bool) && Boolean.TryParse(currentValue.ToString(), out boolResult))
						currentValue = boolResult;
					else if (targetValueType == typeof(Uri) && Uri.TryCreate(currentValue.ToString(), UriKind.RelativeOrAbsolute, out uriResult))
						currentValue = uriResult;
					else if (targetValueType == typeof(string))
						currentValue = currentValue.ToString();
					else if (TryParse(currentValue, targetValueType, out parseRes))
						currentValue = parseRes;

					#endregion

					if (currentValue != null && targetType != currentValue.GetType() && targetType.IsNullableType())
					{
						var nullableConv = new NullableConverter(targetType);
						if (nullableConv.CanConvertFrom(currentValue.GetType()))
						{
							try
							{
								var res = nullableConv.ConvertFrom(currentValue);
								return res;
							}
							catch
							{ }
						}
					}
				}
			}
			return currentValue;
		}

		private bool ShouldUseDataContractSerializer(Type resultType)
		{
			if (classMappingSettings.AutoCheckForDataContractJsonSerializer)
			{
				// Bei liste oder array den inneren typen benutzen
				if (typeof(IEnumerable).IsAssignableFrom(resultType) || resultType.IsArray)
					resultType = ReflectionHelper.GetDeclaringTypes(resultType).FirstOrDefault() ?? resultType;

				return resultType.GetAttributes<DataContractAttribute>(true).Any()
						&& (resultType.GetMembers().Any(info => info.GetAttributes<DataMemberAttribute>(true).Any())
							|| resultType.GetProperties().Any(info => info.GetAttributes<DataMemberAttribute>(true).Any()));
			}
			return false;
		}

		private object FromJson(string json, Type resultType)
		{
			if (ShouldUseDataContractSerializer(resultType))
			{
				var serializer = new DataContractJsonSerializer(resultType);
				MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json));
				return serializer.ReadObject(ms);
			}

            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, resultType);
        }

		private string ToJson(object o)
		{
			return o.JsonSerialize();
		}


		private bool TryParse(object currentValue, Type targetValueType, out object res)
		{
			res = null;
			if (classMappingSettings.SearchForTryParseInTargetTypes)
			{
				var method = targetValueType.GetMethods().FirstOrDefault(info => info.Name == "TryParse" && info.ReturnType == typeof(bool) && info.GetParameters().Count() == 2);
				if (method != null)
				{
					object[] objects = { currentValue.ToString(), null };
					object invoke = method.Invoke(null, objects);
					res = objects[1];
					return (bool)invoke;
				}
			}
			return false;
		}

		private bool TryGuidConversion(object currentValue, Type targetType, out object convertedValue)
		{
			Guid tmp;
			convertedValue = currentValue;
			if (targetType == typeof(Guid) && currentValue is long)
			{
				convertedValue = ((long)currentValue).ToGuid();
				return true;
			}
			if (targetType == typeof(long) && currentValue is Guid)
			{
				convertedValue = ((Guid)currentValue).ToInt64();
				return true;
			}
			if (targetType == typeof(Guid) && currentValue is uint)
			{
				convertedValue = (Convert.ToInt32((uint)currentValue)).ToGuid();
				return true;
			}
			if (targetType == typeof(Guid) && currentValue is int)
			{
				convertedValue = ((int)currentValue).ToGuid();
				return true;
			}
			if (targetType == typeof(int) && currentValue is Guid)
			{
				convertedValue = ((Guid)currentValue).ToInt();
				return true;
			}
			if (targetType == typeof(uint) && currentValue is Guid)
			{
				convertedValue = (uint)Math.Abs(((Guid)currentValue).ToInt());
				return true;
			}
			if (targetType == typeof(Guid) && currentValue is string && !Guid.TryParse(currentValue.ToString(), out tmp))
			{
				convertedValue = currentValue.GetHashCode().ToGuid();
				return true;
			}
			return false;
		}

		private void FillResultListAsync(IEnumerable enumerable, Type[] targetValueTypesForEnumerable, IList resultList)
		{
			var tasks = new List<Task>();
			foreach (var value in enumerable)
			{
				object value1 = value;
				var task = new Task<object>(() => GetConvertedValue(value1, targetValueTypesForEnumerable[0]),
					TaskCreationOptions.AttachedToParent);
				task.ContinueWith(t =>
				{
					lock (lockObject)
					{
						resultList.Add(t.Result);
					}
				});
				tasks.Add(task);
				task.Start();
			}
			Task.WaitAll(tasks.ToArray());
		}


		private void SetValue(string propName, object obj, object propValue)
		{
			try
			{
				object valueToSet;
				var prop = GetProperty(propName, obj);
				if (prop != null && prop.CanWrite)
				{
					valueToSet = GetConvertedValue(propValue, prop.PropertyType);
					prop.SetValue(obj, valueToSet, null);
				}
				else
				{
					var field = GetField(propName, obj);
					if (field != null)
					{
						valueToSet = GetConvertedValue(propValue, field.FieldType);
						field.SetValue(obj, valueToSet);
					}
				}
			}
			catch (ArgumentException)
			{
				if (!classMappingSettings.IgnoreExceptions)
				{
					throw;
				}
			}
		}


		/// <summary>
		/// Gets the property.
		/// </summary>
		private object GetPropertyOrFieldValue(string propertyName, object input)
		{
			string[] propertyNames = propertyName.Split(new[] { '.' });
			if (propertyNames.Count() > 1)
			{
				object childItem = GetProperty(propertyNames[0], input).GetValue(input, null);
				if (childItem != null)
				{
					return GetPropertyOrFieldValue(propertyName.Substring(propertyName.IndexOf(".", StringComparison.Ordinal) + 1), childItem);
				}
				object childItemField = GetField(propertyNames[0], input).GetValue(input);
				if (childItemField != null)
					return GetPropertyOrFieldValue(propertyName.Substring(propertyName.IndexOf(".", StringComparison.Ordinal) + 1), childItemField);
			}
			else
			{
				var p = GetProperty(propertyNames[0], input);
				if (p != null)
					return p.GetValue(input, null);
				var f = GetField(propertyNames[0], input);
				if (f != null)
					return f.GetValue(input);
			}
			return null;
		}

		/// <summary>
		/// Gets the property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		private PropertyInfo GetProperty(string propertyName, object item)
		{
			if (item != null)
			{
				//PropertyInfo subPropertyInfo = item.GetType().GetProperty(propertyName, ReflectionHelper.PublicBindingFlags);
				PropertyInfo subPropertyInfo = item.GetType().GetPropertiesRecursive(GetBindingFlags()).FirstOrDefault(info => info.Name == propertyName);
				return subPropertyInfo;
			}
			return null;
		}
		/// <summary>
		/// Gets the property.
		/// </summary>
		private FieldInfo GetField(string propertyName, object item)
		{
			if (item != null)
			{
				//FieldInfo subPropertyInfo = item.GetType().GetField(propertyName, ReflectionHelper.PublicBindingFlags);
				FieldInfo subPropertyInfo = item.GetType().GetFieldsRecursive(GetBindingFlags()).FirstOrDefault(info => info.Name == propertyName);
				return subPropertyInfo;
			}
			return null;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{ }

	}
}