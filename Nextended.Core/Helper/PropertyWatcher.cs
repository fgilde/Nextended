using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper
{
	/// <summary>
	/// PropertyWatcher
	/// </summary>
	public class PropertyWatcher: IDisposable
	{
		private readonly Dictionary<string,PropertyInfo> properties;
		private readonly Dictionary<string, object> values;

		private readonly object instanceToWatch;
		private readonly Type instanceType;
		private readonly BindingFlags bindingInfo;
		private CancellationTokenSource cancellationTokenSource;

		private SynchronizationContext synchronizationContext;

		/// <summary>
		/// Gibt an ob die Eigenschaft überwacht wird
		/// </summary>
		public bool IsWatching { get; private set; }

		/// <summary>
		/// Wird ausgelöst sobald sich die Eigenschaft ändert
		/// </summary>
		public event EventHandler<PropertyChangedEventArgs<object>> PropertyChanged;


		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyWatcher"/> class.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="prop">The prop.</param>
		public PropertyWatcher(object instance,Expression<Func<object>> prop)
			:this(instance,prop.GetMemberName())
		{}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyWatcher"/> class.
		/// </summary>
		public PropertyWatcher(object instance, string propertyName = "")
		{
			
			bindingInfo = BindingFlags.Public | BindingFlags.Static;
			instanceType = instance as Type;
			if (instanceType == null)
			{
				bindingInfo = BindingFlags.Public | BindingFlags.Instance;
				instanceType = instance.GetType();
				instanceToWatch = instance;
			}

			properties = new Dictionary<string, PropertyInfo>();
			values = new Dictionary<string, object>();
			if (!string.IsNullOrEmpty(propertyName))
			{
				AddPropertyToWatch(propertyName);
			}
		}

		/// <summary>
		/// Adss all existing properties to watch
		/// </summary>
		public void AddAllProptertiesToWatch()
		{
			var propertyInfos = instanceType.GetProperties(bindingInfo);
			foreach (PropertyInfo property in propertyInfos)
				AddPropertyToWatch(property.Name);
		}

		/// <summary>
		/// Fügt eine weitere Property zur überwachung hinzu
		/// </summary>
		public void AddPropertyToWatch(Expression<Func<object>> property)
		{
			AddPropertyToWatch(property.GetMemberName());
		}

		/// <summary>
		/// Fügt eine weitere Property zur überwachung hinzu
		/// </summary>
		public void AddPropertyToWatch(string propertyName)
		{
			PropertyInfo prop = instanceType.GetProperty((propertyName));
			if (!properties.ContainsKey(propertyName))
				properties.Add(propertyName, prop);
			else
				properties[propertyName] = prop;
		}

		/// <summary>
		/// Startet das überwachen der Eigenschaft
		/// </summary>
		public void StartWatching()
		{
			GetValues();
			IsWatching = true;
			cancellationTokenSource = new CancellationTokenSource();
			synchronizationContext = SynchronizationContext.Current;
			Task.Factory.StartNew(WatchProperty,cancellationTokenSource.Token);
		}

		private void GetValues()
		{
			foreach (KeyValuePair<string, PropertyInfo> pair in properties)
			{
				var value = GetValue(pair.Value);
				if (values.ContainsKey(pair.Key))
					values[pair.Key] = value;
				else
					values.Add(pair.Key,value);
			}
		}

		/// <summary>
		/// Beendet die Überwachung der Eigenschaft
		/// </summary>
		public void StopWatching()
		{
			IsWatching = false;
			cancellationTokenSource.Cancel();
		}

		private void WatchProperty()
		{
			while (IsWatching)
			{
				foreach (KeyValuePair<string, PropertyInfo> property in properties)
				{
					var currentValue = values[property.Key];
					var value = GetValue(property.Value);
					if (!value.Equals(currentValue))
					{
						var res = new PropertyChangedEventArgs<object>(property.Key, currentValue, value,property.Value);
						values[property.Key] = value;
						InvokePropertyChanged(res);
					}
				}

				Thread.Sleep(10);
			}
		}

		private object GetValue(PropertyInfo property)
		{
			return property.GetValue(instanceToWatch, null);
		}

		private void InvokePropertyChanged(PropertyChangedEventArgs<object> e)
		{
			EventHandler<PropertyChangedEventArgs<object>> handler = PropertyChanged;
			if (handler != null && IsWatching)
			{
				if (synchronizationContext != null)
					synchronizationContext.Send(state => handler(this, e),null);
				else
					handler(this, e);
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if(IsWatching)
				StopWatching();
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="PropertyWatcher"/> is reclaimed by garbage collection.
		/// </summary>
		~PropertyWatcher()
		{
			Dispose();
		}

	}

	/// <summary>
	/// PropertyChangedEventArgs
	/// </summary>
	public class PropertyChangedEventArgs<T>: PropertyChangedEventArgs
	{        
		/// <summary>
		/// PropertyInfo
		/// </summary>
		public PropertyInfo Property { get; private set; }

		/// <summary>
		/// Neuer Wert der Eigenschaft
		/// </summary>
		public T NewValue { get; private set; }

		/// <summary>
		/// Alter Wert der Eigenschaft
		/// </summary>
		public T OldValue { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyChangedEventArgs&lt;T&gt;"/> class.
		/// </summary>
		public PropertyChangedEventArgs(string propertyName, T oldValue, T newValue, PropertyInfo propertyInfo)
			: base(propertyName)
		{
			NewValue = newValue;
			OldValue = oldValue;
			Property = propertyInfo;
		}
	}

}