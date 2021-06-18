using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;

namespace Nextended.UI.Classes
{

    /// <summary>
    /// CustomClass
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CustomClass : Component, ICustomTypeDescriptor
    {

        //Private members
        private readonly PropertyDescriptorCollection propertyCollection;
        private int maxLength;

        /// <summary>
        /// MaxLength
        /// </summary>
        public int MaxLength
        {
            get => maxLength;
            set
            {
                if (value > maxLength)
                    maxLength = value;
            }
        }


        /// <summary>
        /// Constructor of CustomClass which initializes the new PropertyDescriptorCollection.
        /// </summary>
        public CustomClass()
        {
            propertyCollection = new PropertyDescriptorCollection(new PropertyDescriptor[] { });
        }

        /// <summary>
        /// Constructor of CustomClass which initializes the new PropertyDescriptorCollection.
        /// </summary>
        public CustomClass(object obj):this()
        {
            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                try
                {
                    object value = propertyInfo.GetValue(obj, new object[] { });
                    string desc = string.Empty;
                    if (value != null)
                        desc = value.GetType().ToString();
                    AddProperty(propertyInfo.Name, value, desc, String.Empty, obj.GetType());
                }
                catch(Exception e)
                {
                    Trace.TraceError(e.Message);
                }
            }
        }

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Adds the property.
        /// </summary>
        public PropertyDescriptor AddProperty(string propName, object propValue, string propDesc, string propCat, Type propType)
        {
            return AddProperty(propName, propValue, propDesc, propCat, propType, false, false);
        }

		/// <summary>
		/// Adds the property.
		/// </summary>
		public void RemoveProperty(string propName)
		{
			var prop = propertyCollection.Find(propName, true);
			propertyCollection.Remove(prop);
		}

		/// <summary>
		/// Adds the property.
		/// </summary>
		public void RemoveProperty(DynamicProperty prop)
		{
			propertyCollection.Remove(prop);
		}


        /// <summary>
        /// Adds the property.
        /// </summary>
        public PropertyDescriptor AddProperty<T>(string propName, object propValue, string propDesc,
                       string propCat, Type propType) where T : UITypeEditor
        {
            var property = new DynamicProperty(propName, propValue, propDesc, propCat,propType, false, false,true,new EditorAttribute(typeof(T), typeof(UITypeEditor)));

            int index = propertyCollection.Add(property);

            MaxLength = propName.Length;
            MaxLength = propValue.ToString().Length;

            return propertyCollection[index];
        }

		

        /// <summary>
        /// Adds a property into the CustomClass.
        /// </summary>
        public PropertyDescriptor AddProperty(string propName, object propValue, string propDesc, string propCat, Type propType, bool isReadOnly, bool isExpandable)
        {
            DynamicProperty property;
            if (propValue != null && (!(propValue is int) && !(propValue is bool) && !(propValue is double) && !(propValue is float) && !(propValue is Color)))
            {
                property = new DynamicProperty(propName, propValue, propDesc, propCat, propType, isReadOnly, isExpandable,true,new EditorAttribute(typeof (PropertyGridTypeEditor),typeof (UITypeEditor))
                    );
            }else
            {
                property = new DynamicProperty(propName, propValue, propDesc, propCat,propType, isReadOnly, isExpandable,true); 
            }

            int index = propertyCollection.Add(property);

            MaxLength = propName.Length;
            if(propValue != null)
                MaxLength = propValue.ToString().Length;

            return propertyCollection[index];
        }

        /// <summary>
        /// Gets the <see cref="CustomClass.DynamicProperty"/> at the specified index.
        /// </summary>
        /// <value></value>
        public DynamicProperty this[int index] => (DynamicProperty)propertyCollection[index];

        //Overloaded Indexer for this class - returns a DynamicProperty by name.
        /// <summary>
        /// Gets the <see cref="CustomClass.DynamicProperty"/> with the specified name.
        /// </summary>
        /// <value></value>
        public DynamicProperty this[string name] => (DynamicProperty)propertyCollection[name];

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string GetClassName()
        {
            return (TypeDescriptor.GetClassName(this, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public AttributeCollection GetAttributes()
        {
            return (TypeDescriptor.GetAttributes(this, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string GetComponentName()
        {
            return (TypeDescriptor.GetComponentName(this, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public TypeConverter GetConverter()
        {
            return (TypeDescriptor.GetConverter(this, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public EventDescriptor GetDefaultEvent()
        {
            return (TypeDescriptor.GetDefaultEvent(this, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptor GetDefaultProperty()
        {
            PropertyDescriptorCollection props = GetAllProperties();

            if (props.Count > 0)
                return (props[0]);
            return (null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="editorBaseType"></param>
        /// <returns></returns>
        public object GetEditor(Type editorBaseType)
        {
            return (TypeDescriptor.GetEditor(this, editorBaseType, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return (TypeDescriptor.GetEvents(this, attributes, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public EventDescriptorCollection GetEvents()
        {
            return (TypeDescriptor.GetEvents(this, true));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return (GetAllProperties());
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties()
        {
            return (GetAllProperties());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pd"></param>
        /// <returns></returns>
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return (this);
        }


        /// <summary>
        ///	Helper method to return the PropertyDescriptorCollection or our Dynamic Properties
        /// </summary>
        /// <returns></returns>
        private PropertyDescriptorCollection GetAllProperties()
        {
            return propertyCollection;
        }


        /// <summary>
        ///	This is the Property class this will be dynamically added to the class at runtime.
        ///	These classes are returned in the PropertyDescriptorCollection of the GetAllProperties
        ///	method of the custom class.
        /// </summary>
        /// <returns></returns>
        public class DynamicProperty : PropertyDescriptor
        {
            private readonly string propName;
            private object propValue;
            private readonly string propDescription;
            private readonly string propCategory;
            private readonly Type propType;
            private readonly bool isReadOnly;
            private readonly bool isExpandable;
            private readonly bool isBrowsable;

            public DynamicProperty(string pName, object pValue, string pDesc, string pCat, Type pType, bool readOnly, bool expandable, bool isBrowsable, params Attribute[] attrs)
                : base(pName, attrs)
            {
                propName = pName;
                propValue = pValue;
                propDescription = pDesc;
                propCategory = pCat;
                propType = pType;
                isReadOnly = readOnly;
                isExpandable = expandable;
                this.isBrowsable = isBrowsable;
            }

         //   public IEnumerable<string> PossibleValues { get; set; }

			/// <summary>
			/// Gets the name of the member.
			/// </summary>
			/// <value></value>
			/// <returns>The name of the member.</returns>
			public override string Name => PropertyName;

            /// <summary>
            /// Name
            /// </summary>
            public string PropertyName => propName;

            /// <summary>
            /// IsExpandable
            /// </summary>
            public bool IsExpandable => isExpandable;

            /// <summary>
            /// When overridden in a derived class, gets the type of the component this property is bound to.
            /// </summary>
            /// <value></value>
            /// <returns>A <see cref="T:System.Type"/> that represents the type of component this property is bound to. When the <see cref="M:System.ComponentModel.PropertyDescriptor.GetValue(System.Object)"/> or <see cref="M:System.ComponentModel.PropertyDescriptor.SetValue(System.Object,System.Object)"/> methods are invoked, the object specified might be an instance of this type.</returns>
            public override Type ComponentType => propValue == null ? null : propValue.GetType();

            /// <summary>
            /// Gets the name of the category to which the member belongs, as specified in the <see cref="T:System.ComponentModel.CategoryAttribute"/>.
            /// </summary>
            /// <value></value>
            /// <returns>The name of the category to which the member belongs. If there is no <see cref="T:System.ComponentModel.CategoryAttribute"/>, the category name is set to the default category, Misc.</returns>
            /// <PermissionSet>
            /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
            /// </PermissionSet>
            public override string Category => propCategory;

            /// <summary>
            /// Gets a value indicating whether the member is browsable, as specified in the <see cref="T:System.ComponentModel.BrowsableAttribute"/>.
            /// </summary>
            /// <value></value>
            /// <returns>true if the member is browsable; otherwise, false. If there is no <see cref="T:System.ComponentModel.BrowsableAttribute"/>, the property value is set to the default, which is true.</returns>
            public override bool IsBrowsable => isBrowsable;

            /// <summary>
            /// When overridden in a derived class, gets a value indicating whether this property is read-only.
            /// </summary>
            /// <value></value>
            /// <returns>true if the property is read-only; otherwise, false.</returns>
            public override bool IsReadOnly => isReadOnly;

            /// <summary>
            /// When overridden in a derived class, gets the type of the property.
            /// </summary>
            /// <value></value>
            /// <returns>A <see cref="T:System.Type"/> that represents the type of the property.</returns>
            public override Type PropertyType => propType;

            /// <summary>
            /// When overridden in a derived class, returns whether resetting an object changes its value.
            /// </summary>
            /// <param name="component">The component to test for reset capability.</param>
            /// <returns>
            /// true if resetting the component changes its value; otherwise, false.
            /// </returns>
            public override bool CanResetValue(object component)
            {
                return true;
            }

            /// <summary>
            /// When overridden in a derived class, gets the current value of the property on a component.
            /// </summary>
            /// <param name="component">The component with the property for which to retrieve the value.</param>
            /// <returns>
            /// The value of a property for a given component.
            /// </returns>
            public override object GetValue(object component)
            {
                return propValue;
            }

            /// <summary>
            /// When overridden in a derived class, sets the value of the component to a different value.
            /// </summary>
            /// <param name="component">The component with the property value that is to be set.</param>
            /// <param name="value">The new value.</param>
            public override void SetValue(object component, object value)
            {
                propValue = value;
            }

            /// <summary>
            /// When overridden in a derived class, resets the value for this property of the component to the default value.
            /// </summary>
            /// <param name="component">The component with the property value that is to be reset to the default value.</param>
            public override void ResetValue(object component)
            {
                propValue = null;
            }

            /// <summary>
            /// When overridden in a derived class, determines a value indicating whether the value of this property needs to be persisted.
            /// </summary>
            /// <param name="component">The component with the property to be examined for persistence.</param>
            /// <returns>
            /// true if the property should be persisted; otherwise, false.
            /// </returns>
            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }

            /// <summary>
            /// Gets the description of the member, as specified in the <see cref="T:System.ComponentModel.DescriptionAttribute"/>.
            /// </summary>
            /// <value></value>
            /// <returns>The description of the member. If there is no <see cref="T:System.ComponentModel.DescriptionAttribute"/>, the property value is set to the default, which is an empty string ("").</returns>
            public override string Description => propDescription;
        }

    }
}