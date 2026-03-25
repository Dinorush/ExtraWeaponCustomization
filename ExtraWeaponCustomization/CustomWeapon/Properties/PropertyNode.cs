using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties
{
    public sealed class PropertyNode
    {
        public readonly PropertyList List;
        public readonly List<PropertyNode> Children;
        public readonly IPropertyHolder? Owner;
        public bool Active { get; set; } = false;
        public bool Enabled { get; set; } = true;
        public bool Override { get; set; } = false;

        public PropertyNode(PropertyList list, PropertyNode? parent, IPropertyHolder? owner)
        {
            List = list;
            Children = new List<PropertyNode>();
            Owner = owner;
            if (Owner != null)
                Owner.Node = this;
            parent?.Children.Add(this);
        }
    }
}
