using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Core
{
    /// <summary>
    /// Extension methods for key-value pairs
    /// </summary>
    public static class KeyValuePairExtensions
    {
        /// <summary>
        /// Converts a key-value pair to a bindable key-value pair
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TValue">The type of the value</typeparam>
        /// <param name="pair">The key-value pair to convert</param>
        /// <returns>A bindable key-value pair</returns>
        public static BindableKeyValuePair<TKey, TValue> AsBindable<TKey, TValue>(this KeyValuePair<TKey, TValue> pair)
        {
            return pair;
        }

        /// <summary>
        /// Converts a dictionary to an observable collection of bindable key-value pairs
        /// </summary>
        /// <typeparam name="TKey">The type of the keys</typeparam>
        /// <typeparam name="TValue">The type of the values</typeparam>
        /// <param name="dictionary">The dictionary to convert</param>
        /// <returns>An observable collection of bindable key-value pairs</returns>
        public static ObservableCollection<BindableKeyValuePair<TKey, TValue>> ToBindableList<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select(pair => pair.AsBindable()).ToObservableCollection();
        }
	}

	/// <summary>
	/// Represents a bindable key-value pair that supports property change notification
	/// </summary>
	/// <typeparam name="TKey">The type of the key</typeparam>
	/// <typeparam name="TValue">The type of the value</typeparam>
	public class BindableKeyValuePair<TKey, TValue> : NotificationObject
	{
		/// <summary>
		/// Occurs when the key or value changes
		/// </summary>
		public event EventHandler? Changed;

		private TKey key = default!;
		private TValue _value = default!;

		/// <summary>
		/// Gets or sets the key
		/// </summary>
		public TKey Key
		{
			get => key;
            set
			{
				if(SetProperty(ref key, value))
					RaiseChanged();
			}
		}

		/// <summary>
		/// Gets or sets the value
		/// </summary>
		public TValue Value
		{
			get => _value;
            set
			{
				if(SetProperty(ref _value, value))
					RaiseChanged();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BindableKeyValuePair{TKey, TValue}"/> class
		/// </summary>
		public BindableKeyValuePair() { }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BindableKeyValuePair{TKey, TValue}"/> class with the specified key and value
		/// </summary>
		/// <param name="key">The key</param>
		/// <param name="value">The value</param>
		public BindableKeyValuePair(TKey key, TValue value)
			: this()
		{
			Key = key;
			Value = value;
		}

		/// <summary>
		/// Implicitly converts a bindable key-value pair to a standard key-value pair
		/// </summary>
		/// <param name="keyValuePair">The bindable key-value pair to convert</param>
		public static implicit operator KeyValuePair<TKey, TValue>(BindableKeyValuePair<TKey, TValue> keyValuePair )
		{
			return new KeyValuePair<TKey, TValue>(keyValuePair.Key, keyValuePair.Value);
		}

		/// <summary>
		/// Implicitly converts a standard key-value pair to a bindable key-value pair
		/// </summary>
		/// <param name="pair">The key-value pair to convert</param>
		public static implicit operator BindableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
		{
			return new BindableKeyValuePair<TKey, TValue>(pair.Key, pair.Value);
		}


		private void RaiseChanged()
		{
			var handler = Changed;
            handler?.Invoke(this, EventArgs.Empty);
        }

	}

	/// <summary>
	/// Represents a bindable value that supports property change notification
	/// </summary>
	/// <typeparam name="TValue">The type of the value</typeparam>
	public class Bindable<TValue> : NotificationObject
	{
		/// <summary>
		/// Occurs when the value changes
		/// </summary>
		public event EventHandler? Changed;

		private TValue _value = default!;
		
		/// <summary>
		/// Gets or sets the value
		/// </summary>
		public TValue Value
		{
			get => _value;
            set
			{
				if (SetProperty(ref _value, value))
					RaiseChanged();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Bindable{TValue}"/> class
		/// </summary>
		public Bindable() { }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Bindable{TValue}"/> class with the specified value
		/// </summary>
		/// <param name="value">The initial value</param>
		public Bindable(TValue value)
			: this()
		{			
			Value = value;
		}

		/// <summary>
		/// Implicitly converts a bindable value to its underlying value
		/// </summary>
		/// <param name="value">The bindable value to convert</param>
		public static implicit operator TValue(Bindable<TValue> value)
		{
			return value.Value;
		}

		/// <summary>
		/// Implicitly converts a value to a bindable value
		/// </summary>
		/// <param name="value">The value to convert</param>
		public static implicit operator Bindable<TValue>(TValue value)
		{
			return new Bindable<TValue>(value);
		}

		private void RaiseChanged()
		{
			var handler = Changed;
            handler?.Invoke(this, EventArgs.Empty);
        }

	}

}