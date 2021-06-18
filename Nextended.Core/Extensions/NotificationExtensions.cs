using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Nextended.Core.Extensions
{
    public static class NotificationExtensions
    {
        public static TNotificationObject OnChange<TNotificationObject>(this TNotificationObject propertyChangedObject, Action<TNotificationObject> callback)
            where TNotificationObject : INotifyPropertyChanged
        {
            propertyChangedObject.PropertyChanged += (sender, args) =>
            {
                callback?.Invoke(propertyChangedObject);
            };
            return propertyChangedObject;
        }

        public static void OnChange<TPropertyType>(this INotifyPropertyChanged propertyChangedObject, Expression<Func<TPropertyType>> action, Action<TPropertyType> callback)
        {
            propertyChangedObject.PropertyChanged += (sender, args) =>
            {
                if (args?.PropertyName != null && action != null && args.PropertyName == action.GetMemberName())
                {
                    Func<TPropertyType> func = action.Compile();
                    callback?.Invoke(func());
                }
            };
        }
	}
}