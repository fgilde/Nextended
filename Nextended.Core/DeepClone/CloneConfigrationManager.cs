using System;

namespace Nextended.Core.DeepClone
{
    /// <summary>
    /// globa ConfigrationManager, for managing the library settings
    /// </summary>
    public static class CloneConfigrationManager
    {
        // this will trigger when a new PropertyInfo, FieldInfo Type is applied to IFastDeepClonerProperty
        // you could handle which type the IFastDeepClonerProperty PropertyType should containe.
        public static Func<IClonerProperty, Type> OnPropertTypeSet;

        // This will trigger when a new attribute is added, you could make some changes to IFastDeepClonerProperty
        public static Action<IClonerProperty> OnAttributeCollectionChanged;

    }
}
