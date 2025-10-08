using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Core
{
    public static class KeyValuePairExtensions
    {
        public static BindableKeyValuePair<TKey, TValue> AsBindable<TKey, TValue>(this KeyValuePair<TKey, TValue> pair)
        {
            return pair;
        }

        public static ObservableCollection<BindableKeyValuePair<TKey, TValue>> ToBindableList<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select(pair => pair.AsBindable()).ToObservableCollection();
        }
	}

	public class BindableKeyValuePair<TKey, TValue> : NotificationObject
	{
		public event EventHandler? Changed;

		private TKey key = default!;
		private TValue _value = default!;

		public TKey Key
		{
			get => key;
            set
			{
				if(SetProperty(ref key, value))
					RaiseChanged();
			}
		}

		public TValue Value
		{
			get => _value;
            set
			{
				if(SetProperty(ref _value, value))
					RaiseChanged();
			}
		}

		public BindableKeyValuePair() { }
		public BindableKeyValuePair(TKey key, TValue value)
			: this()
		{
			Key = key;
			Value = value;
		}

		public static implicit operator KeyValuePair<TKey, TValue>(BindableKeyValuePair<TKey, TValue> keyValuePair )
		{
			return new KeyValuePair<TKey, TValue>(keyValuePair.Key, keyValuePair.Value);
		}

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

	public class Bindable<TValue> : NotificationObject
	{
		public event EventHandler? Changed;

		private TValue _value = default!;
		
		public TValue Value
		{
			get => _value;
            set
			{
				if (SetProperty(ref _value, value))
					RaiseChanged();
			}
		}

		public Bindable() { }
		public Bindable(TValue value)
			: this()
		{			
			Value = value;
		}

		public static implicit operator TValue(Bindable<TValue> value)
		{
			return value.Value;
		}

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