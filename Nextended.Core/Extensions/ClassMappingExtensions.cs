using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Nextended.Core.Helper;

namespace Nextended.Core.Extensions
{
	/// <summary>
	/// Extension methods for object-to-object mapping. Provides fluent API for converting objects between different types
	/// with support for custom converters, property assignments, and asynchronous operations.
	/// </summary>
	public static class ClassMappingExtensions
	{

        private static ClassMapper Mapper(ClassMappingSettings settings)
        {
            if (ClassMapper.defaultClassMapperInstance != null)
            {
                var result = ClassMapper.defaultClassMapperInstance;
                if (settings != null) // Only set settings if we get some, otherwise instance settings should be used
                    result.SetSettings(settings);
                return result;
            }
            return new ClassMapper().SetSettings(settings ?? ClassMappingSettings.Default);
        }

		/// <summary>
		/// Maps an object to a specified target type.
		/// Automatically converts properties to match the target type and handles nested objects, collections, and type conversions.
		/// </summary>
		/// <typeparam name="TInput">The type of the input object.</typeparam>
		/// <param name="input">The object to map from.</param>
		/// <param name="tResult">The target type to map to.</param>
		/// <param name="settings">Optional mapping settings to control the mapping behavior. Uses default settings if null.</param>
		/// <returns>A new instance of the target type with mapped properties.</returns>
		public static object MapTo<TInput>(this TInput input, Type tResult, ClassMappingSettings settings = null)
        {
            return Mapper(settings).Map(input, tResult);
		}

        /// <summary>
        /// Asynchronously maps an object to a specified target type.
        /// Useful for mapping large objects or when you want to perform the mapping on a background thread.
        /// </summary>
        /// <typeparam name="TInput">The type of the input object.</typeparam>
        /// <param name="input">The object to map from.</param>
        /// <param name="tResult">The target type to map to.</param>
        /// <param name="settings">Optional mapping settings to control the mapping behavior.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the mapped object.</returns>
		public static Task<object> MapToAsync<TInput>(this TInput input, Type tResult, ClassMappingSettings settings = null)
        {
            return Mapper(settings).MapAsync(input, tResult);
		}

        /// <summary>
        /// Maps an object to a target type with custom property assignments.
        /// Allows you to specify additional mapping logic for properties that need special handling.
        /// </summary>
        /// <typeparam name="TInput">The type of the input object.</typeparam>
        /// <typeparam name="TResult">The type of the result object.</typeparam>
        /// <param name="input">The object to map from.</param>
        /// <param name="settings">Mapping settings to control the mapping behavior.</param>
		/// <param name="differentMappingAssignments">One or more custom assignment actions in the form (result, input) => result.Property = input.OtherProperty</param>
		/// <returns>A new instance of TResult with mapped properties and custom assignments applied.</returns>
		public static TResult MapTo<TInput, TResult>(this TInput input, ClassMappingSettings settings,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return Mapper(settings).Map(input, differentMappingAssignments);
		}

		/// <summary>
		/// Maps an object to a target type with custom property assignments using default settings.
		/// </summary>
		/// <typeparam name="TResult">The type of the result object.</typeparam>
		/// <typeparam name="TInput">The type of the input object.</typeparam>
		/// <param name="input">The object to map from.</param>
		/// <param name="differentMappingAssignments">One or more custom assignment actions for special property mappings.</param>
		/// <returns>A new instance of TResult with mapped properties and custom assignments applied.</returns>
		public static TResult MapTo<TInput, TResult>(this TInput input,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return input.MapTo(null, differentMappingAssignments);
		}

        /// <summary>
        /// Maps an object to a target type using specific type converters for this operation only.
        /// </summary>
        /// <typeparam name="TResult">The type of the result object.</typeparam>
        /// <param name="input">The object to map from.</param>
		/// <param name="specificConverters">One or more type converters to use for this mapping operation.</param>
		/// <returns>A new instance of TResult with mapped properties.</returns>
		public static TResult MapTo<TResult>(this object input, params TypeConverter[] specificConverters)
		{
			return MapTo<object, TResult>(input, ClassMappingSettings.Default.Set(s => s.AddConverters(specificConverters)));
		}

		/// <summary>
		/// Maps an object to a target type with specified settings.
		/// </summary>
		/// <typeparam name="TResult">The type of the result object.</typeparam>
        /// <param name="input">The object to map from.</param>
		/// <param name="settings">Mapping settings to control the mapping behavior.</param>
		/// <returns>A new instance of TResult with mapped properties.</returns>
		public static TResult MapTo<TResult>(this object input, ClassMappingSettings settings)
		{
			return MapTo<object, TResult>(input, settings);
		}


        /// <summary>
        /// Map class to another
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <typeparam name="TInput">Input object type</typeparam>
        /// <param name="input">Input instance</param>
        /// <param name="settings">Settings</param>
        /// <param name="differentMappingAssignments">Extra mapping assignments (z.B o1,o2=> o1.Name = 02.FirstName)</param>
        public static Task<TResult> MapToAsync<TInput, TResult>(this TInput input, ClassMappingSettings settings,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return Mapper(settings).MapAsync(input, differentMappingAssignments);
		}

        /// <summary>
        /// Maps each element in a collection to the specified target type.
        /// Useful for converting entire collections like List&lt;SourceDto&gt; to List&lt;TargetEntity&gt;.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="enumerable">The collection to map.</param>
        /// <param name="settings">Optional mapping settings. Uses default settings if null.</param>
        /// <returns>An enumerable of mapped elements.</returns>
        public static IEnumerable<T> MapElementsTo<T>(this IEnumerable enumerable, ClassMappingSettings settings = null)
		{
			return enumerable.MapTo<IEnumerable<T>>(settings ?? ClassMappingSettings.Default);
		}



        /// <summary>
        /// Map class to another
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <typeparam name="TInput">Input object type</typeparam>
        /// <param name="input">Input instance</param>
        /// <param name="differentMappingAssignments">Extra mapping assignments (z.B o1,o2=> o1.Name = 02.FirstName)</param>
		public static Task<TResult> MapToAsync<TInput, TResult>(this TInput input,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return input.MapToAsync(null, differentMappingAssignments);
		}

		/// <summary>
		/// Map class to another
		/// </summary>
		/// <typeparam name="TResult">Result type</typeparam>
        /// <param name="input">Input instance</param>
		/// <param name="specificConverters">Specific converters for this conversion</param>
        public static Task<TResult> MapToAsync<TResult>(this object input, params TypeConverter[] specificConverters)
		{
			return MapToAsync<object, TResult>(input, ClassMappingSettings.Default.Set(s => s.AddConverters(specificConverters)));
		}

		/// <summary>
		/// Map class to another
		/// </summary>
		/// <typeparam name="TResult">Result type</typeparam>
		/// <param name="input">Input instance</param>
        /// <param name="settings">Settings</param>
        public static Task<TResult> MapToAsync<TResult>(this object input, ClassMappingSettings settings)
		{
			return MapToAsync<object, TResult>(input, settings);
		}

		/// <summary>
		///  Set settings on class mapping settings
		/// </summary>
		public static ClassMappingSettings Set<T>(this ClassMappingSettings settings,
			Expression<Func<ClassMappingSettings, T>> memberExpression, T value)
		{
			string memberName = memberExpression.GetMemberName();
			var prop = Check.NotNull(() => settings.GetType().GetProperty(memberName));
			prop.SetValue(settings, value, null);
			return settings;
		}

		/// <summary>
		/// Set settings on class mapping settings
		/// </summary>
		public static ClassMappingSettings Set(this ClassMappingSettings settings, params Action<ClassMappingSettings>[] o)
		{
			o.Apply(action => action(settings));
			return settings;
		}

		/// <summary>
		/// return settings
		/// </summary>
		public static ClassMappingSettings Settings(
			this Tuple<ClassMappingSettings, MemberInfo, MemberInfo> tpl)
		{
			return tpl.Item1;
		}

		/// <summary>
		/// return settings
		/// </summary>
		public static ClassMappingSettings Settings(
			this Tuple<ClassMappingSettings, MemberInfo> tpl)
		{
			return tpl.Item1;
		}

		/// <summary>
		/// Adds Property assignment if properties are different
		/// </summary>
		public static Tuple<ClassMappingSettings, MemberInfo> Assign<TInput>(this ClassMappingSettings settings,
			Expression<Func<TInput, object>> inProp)
		{
			return Tuple.Create(settings, inProp.GetMemberInfo());
		}

		/// <summary>
		/// Assignment To (monade)
		/// </summary>
		public static Tuple<ClassMappingSettings, MemberInfo, MemberInfo> To<TResult>(this Tuple<ClassMappingSettings, MemberInfo> tpl,
			Expression<Func<TResult, object>> outProp)
		{
			var memberInfo = outProp.GetMemberInfo();
			return Tuple.Create(tpl.Item1.AddAssignment(tpl.Item2, memberInfo), tpl.Item2, memberInfo);
		}

		/// <summary>
		/// Assignment And (monade)
		/// </summary>
		public static Tuple<ClassMappingSettings, MemberInfo, MemberInfo> And<TResult>(this Tuple<ClassMappingSettings, MemberInfo, MemberInfo> tpl,
			Expression<Func<TResult, object>> outProp)
		{
			var memberInfo = outProp.GetMemberInfo();
			return Tuple.Create(tpl.Item1.AddAssignment(tpl.Item2, memberInfo), tpl.Item2, memberInfo);
		}

	}
}