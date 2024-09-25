using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Nextended.Core
{
	/// <summary>
	/// <see cref="INotifyPropertyChanged"/> und <see cref="INotifyPropertyChanging"/> implementierung
	/// </summary>
	[Serializable]
	[DataContract]
	public abstract class NotificationObject : INotifyPropertyChanged, INotifyPropertyChanging, ICloneable
	{
		private bool isNotifying;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationObject"/> class.
		/// </summary>
		protected NotificationObject()
		{
			IsNotifying = true;
		}

		/// <summary>
		/// Enables/Disables property change notification.
		/// </summary>
		public bool IsNotifying
		{
			get => isNotifying;
            set
			{
				isNotifying = value;
				RaiseIsNotifyingChanged();
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Occurs when a property value is changing.
		/// </summary>
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>
		/// Occurs when [IsNotifying changed].
		/// </summary>
		public event EventHandler IsNotifyingChanged;

		/// <summary>
		/// Called when [property changed].
		/// </summary>
		protected virtual void RaisePropertyChanged(Expression<Func<object>> action)
		{
			RaisePropertyChanged(GetMemberName(action));
		}

		/// <summary>
		/// Called when [property changing].
		/// </summary>
		protected virtual void RaisePropertyChanging(Expression<Func<object>> action)
		{
			RaisePropertyChanging(GetMemberName(action));
		}

		/// <summary>
		/// Raises for all properties the propertychanged.
		/// </summary>
		protected virtual void RaiseAllPropertiesChanged()
		{
			var properties = GetType().GetProperties();
			foreach (PropertyInfo property in properties)
				RaisePropertyChanged(property.Name);
		}

		/// <summary>
		/// Raises for all properties the propertychanged.
		/// </summary>
		protected virtual void RaiseAllPropertiesChanging()
		{
			var properties = GetType().GetProperties();
			foreach (PropertyInfo property in properties)
				RaisePropertyChanging(property.Name);
		}

		/// <summary>
		/// Raises the property changing.
		/// </summary>
		protected virtual void RaisePropertyChanging(string propertyName)
		{
			PropertyChangingEventHandler handler = PropertyChanging;
			if (handler != null && IsNotifying)
				handler(this, new PropertyChangingEventArgs(propertyName));
		}

		/// <summary>
		/// Raises the property changed.
		/// </summary>
		protected virtual void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null && IsNotifying)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		private void RaiseIsNotifyingChanged()
		{
			EventHandler handler = IsNotifyingChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Nameof property
		///   This
		/// value is optional and can be provided automatically</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
            RaisePropertyChanging(propertyName);
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyMember">Member of the property used to notify listeners. ()=>PropName
		///   This
		/// value is optional and can be provided automatically</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, Expression<Func<object>> propertyMember)
        {
            return SetProperty(ref storage, value, GetMemberName(propertyMember));
        }

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public virtual object Clone()
		{
			return MemberwiseClone();
		}

		/// <summary> 
		/// Helper method to get member name with compile time verification to avoid typo. 
		/// </summary> 
		/// <param name="expr">The lambda expression usually in the form of o => o.member.</param> 
		/// <returns>The name of the property.</returns> 
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Not used in all design time assemblies.")]
		private static string GetMemberName<T>(Expression<Func<T>> expr)
		{
			Expression body = expr.Body;
			if (body is MemberExpression || body is UnaryExpression)
			{
				MemberExpression memberExpression = body as MemberExpression ?? (MemberExpression)((UnaryExpression)body).Operand;
				return memberExpression.Member.Name;
			}
			//MemberInfo memberInfo = GetMemberInfo(expr);

			return expr.ToString();
		}

	}
}