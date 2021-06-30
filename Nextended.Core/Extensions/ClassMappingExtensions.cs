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
	/// Classmapping extensions
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
		/// Map class to another
		/// </summary>
		/// <param name="input">Entry object</param>
		/// <param name="tResult">Type of result</param>
		/// <param name="settings">Classmapping settings</param>
		public static object MapTo<TInput>(this TInput input, Type tResult, ClassMappingSettings settings = null)
        {
            return Mapper(settings).Map(input, tResult);
		}

        /// <summary>
        /// Map class to another
        /// </summary>
        /// <param name="input">Entry object</param>
        /// <param name="tResult">Type of result</param>
        /// <param name="settings">Classmapping settings</param>
		public static Task<object> MapToAsync<TInput>(this TInput input, Type tResult, ClassMappingSettings settings = null)
        {
            return Mapper(settings).MapAsync(input, tResult);
		}

        /// <summary>
        /// Map class to another
        /// </summary>
        /// <param name="input">Entry object</param>
        /// <param name="tResult">Type of result</param>
        /// <param name="settings">Classmapping settings</param>
		/// <param name="differentMappingAssignments">Extra mapping assignments (example o1,o2=> o1.Name = 02.FirstName)</param>
		public static TResult MapTo<TInput, TResult>(this TInput input, ClassMappingSettings settings,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return Mapper(settings).Map(input, differentMappingAssignments);
		}

		/// <summary>
		/// Map class to another
		/// </summary>
		/// <typeparam name="TResult">Result type</typeparam>
		/// <typeparam name="TInput">Input object type</typeparam>
		/// <param name="input">Input instance</param>
		/// <param name="differentMappingAssignments">Extra mapping assignments (z.B o1,o2=> o1.Name = 02.FirstName)</param>
		public static TResult MapTo<TInput, TResult>(this TInput input,
			params Action<TResult, TInput>[] differentMappingAssignments)
		{
			return input.MapTo(null, differentMappingAssignments);
		}

        /// <summary>
        /// Map class to another
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="input">Input instance</param>
		/// <param name="specificConverters">Specific converters for this conversion</param>
		public static TResult MapTo<TResult>(this object input, params TypeConverter[] specificConverters)
		{
			return MapTo<object, TResult>(input, ClassMappingSettings.Default.Set(s => s.AddConverters(specificConverters)));
		}

		/// <summary>
		/// Map class to another
		/// </summary>
		/// <typeparam name="TResult">Result type</typeparam>
        /// <param name="input">Entry object</param>
		/// <param name="settings">Classmapping settings</param>
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
        /// Map each element in enumerable
        /// </summary>
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