using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace Nextended.Core
{
	/// <summary>
	/// Base Notification Object, that automatic correct implements IEditable
	/// </summary>
	[DataContract]
    public abstract class EditableNotificationObject : NotificationObject, IEditableObject
    {
        private Dictionary<string, object> cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="EditableNotificationObject"/> class.
		/// </summary>
        protected EditableNotificationObject()
        {
            cache = new Dictionary<string, object>();
        }

        /// <summary>
        /// Begins an edit on an object.
        /// </summary>
        public virtual void BeginEdit()
        {
            cache ??= new Dictionary<string, object>();
            cache.Clear();
            foreach (PropertyInfo property in GetType().GetProperties())
                cache.Add(property.Name, property.GetValue(this, null));
        }

        /// <summary>
        /// Pushes changes since the last <see cref="M:System.ComponentModel.IEditableObject.BeginEdit"/> or <see cref="M:System.ComponentModel.IBindingList.AddNew"/> call into the underlying object.
        /// </summary>
        public virtual void EndEdit()
        {
            cache.Clear();
        }

        /// <summary>
        /// Discards changes since the last <see cref="M:System.ComponentModel.IEditableObject.BeginEdit"/> call.
        /// </summary>
        public virtual void CancelEdit()
        {
            if (cache != null && cache.Count > 0)
            {
                foreach (KeyValuePair<string, object> p in cache)
                {
                    PropertyInfo property = GetType().GetProperty(p.Key);
                    if (property != null && property.CanWrite)
                        property.SetValue(this, p.Value, null);
                }
            }
        }
    }
}