using System;
using System.ComponentModel;
using System.Globalization;

namespace Nextended.Core.TypeConverters
{
	/// <summary>
	/// Konverter mit konverter func, kann z.B für serializer und classmapper benutzt werden
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	public class GenericTypeConverter<TIn, TOut> : SimpleFuncConverter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.ComponentModel.TypeConverter"/> class. 
		/// </summary>
		public GenericTypeConverter(Func<TIn, TOut> converterFunc, bool allowAssignableInputs)
			: base(typeof(TIn), typeof(TOut), o => converterFunc((TIn)o), allowAssignableInputs)
		{ }

		/// <summary>
		/// Sets the converter func.
		/// </summary>
		public void SetConverterFunc(Func<TIn, TOut> fn)
		{
			base.SetConverterFunc(o => fn((TIn)o));
		}
	}


	/// <summary>
	/// Konverter mit konverter func, kann z.B für serializer und classmapper benutzt werden
	/// </summary>
	public class SimpleFuncConverter : System.ComponentModel.TypeConverter
	{
		private readonly Type tIn;
		private readonly Type tOut;
		private Func<object, object> converterFunc;
		private readonly bool allowAssignableInputs;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.ComponentModel.TypeConverter"/> class. 
		/// </summary>
		public SimpleFuncConverter(Type tIn, Type tOut, Func<object, object> converterFunc, bool allowAssignableInputs)
		{
			this.tIn = tIn;
			this.tOut = tOut;
			this.converterFunc = converterFunc;
			this.allowAssignableInputs = allowAssignableInputs;
		}

		/// <summary>
		/// Sets the converter func.
		/// </summary>
		public void SetConverterFunc(Func<object, object> fn)
		{
			converterFunc = fn;
		}

		/// <summary>
		/// Converts the given value object to the specified type, using the specified context and culture information.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Object"/> that represents the converted value.
		/// </returns>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context. </param><param name="culture">A <see cref="T:System.Globalization.CultureInfo"/>. If null is passed, the current culture is assumed. </param><param name="value">The <see cref="T:System.Object"/> to convert. </param><param name="destinationType">The <see cref="T:System.Type"/> to convert the <paramref name="value"/> parameter to. </param><exception cref="T:System.ArgumentNullException">The <paramref name="destinationType"/> parameter is null. </exception><exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			return converterFunc(value);
		}

		/// <summary>
		/// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
		/// </summary>
		/// <returns>
		/// true if this converter can perform the conversion; otherwise, false.
		/// </returns>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context. </param><param name="sourceType">A <see cref="T:System.Type"/> that represents the type you want to convert from. </param>
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (allowAssignableInputs)
			{
				//return sourceType.IsAssignableFrom(typeof(TIn));				
				return tIn.IsAssignableFrom(sourceType);
			}
			return sourceType == tIn;
		}

		/// <summary>
		/// Returns whether this converter can convert the object to the specified type, using the specified context.
		/// </summary>
		/// <returns>
		/// true if this converter can perform the conversion; otherwise, false.
		/// </returns>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context. </param><param name="destinationType">A <see cref="T:System.Type"/> that represents the type you want to convert to. </param>
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.IsAssignableFrom(tOut);
		}
	}
}