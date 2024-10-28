using System;
using System.Collections.Generic;

namespace Nextended.Core.DeepClone
{
    public class AttributesCollections : List<Attribute>
    {
        internal SafeValueType<Attribute, Attribute> ContainedAttributes = new();
        internal SafeValueType<Type, Attribute> ContainedAttributesTypes = new();

        public AttributesCollections(List<Attribute> attrs)
        {
            if (attrs == null)
                return;
            foreach (var attr in attrs)
                Add(attr);
        }

        public new void Add(Attribute attr)
        {
            ContainedAttributes.TryAdd(attr, attr, true);
            ContainedAttributesTypes.TryAdd(attr.GetType(), attr, true);
            base.Add(attr);

        }

        public new void Remove(Attribute attr)
        {
            base.Remove(attr);
            ContainedAttributes.TryRemove(attr, out _);
            ContainedAttributesTypes.TryRemove(attr.GetType(), out _);
        }

    }
}
