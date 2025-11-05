using System;
using Nextended.Core.Helper;

namespace Nextended.Core
{
	/// <summary>
	/// Class that implements automatic INotifyPropertyCHanged an INotifyPropertyChanging 
	/// with default {get; set} properties
	/// </summary>
	public abstract class AutoEditableNotificationObject 
		: EditableNotificationObject
	{
		private readonly PropertyWatcher watcher;

		/// <summary>
		/// Occurs when a property changes, providing detailed information about the change
		/// </summary>
		public event EventHandler<PropertyChangedEventArgs<object>> PropertyChangedDetailed;

		/// <summary>
		/// Initializes a new instance of the <see cref="EditableNotificationObject"/> class.
		/// </summary>
		protected AutoEditableNotificationObject()
		{
			watcher = new PropertyWatcher(this);
			watcher.AddAllProptertiesToWatch();
			watcher.PropertyChanged += WatcherOnPropertyChanged;
			if(IsNotifying)
				watcher.StartWatching();
			IsNotifyingChanged += OnIsNotifyingChanged;
		}

		private void WatcherOnPropertyChanged(object sender, 
			PropertyChangedEventArgs<object> propertyChangedEventArgs)
		{
			RaisePropertyChanged(propertyChangedEventArgs.PropertyName);
			RaisePropertyChangedDetailed(propertyChangedEventArgs);
		}

		private void OnIsNotifyingChanged(object sender, EventArgs eventArgs)
		{
			if(IsNotifying && !watcher.IsWatching)
				watcher.StartWatching();
			if(!IsNotifying && watcher.IsWatching)
				watcher.StopWatching();
		}

		private void RaisePropertyChangedDetailed(PropertyChangedEventArgs<object> e)
		{
			EventHandler<PropertyChangedEventArgs<object>> handler = PropertyChangedDetailed;
            handler?.Invoke(this, e);
        }
	}
}