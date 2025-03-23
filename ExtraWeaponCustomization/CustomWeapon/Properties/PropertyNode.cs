using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties
{
    internal sealed class PropertyNode
    {
        public readonly PropertyList List;
        public readonly List<PropertyNode> Children;
        public bool Active { get; set; } = false;
        public bool Enabled { get; set; } = true;

        public PropertyNode(PropertyList list, PropertyNode? parent)
        {
            List = list;
            Children = new List<PropertyNode>();
            parent?.Children.Add(this);
        }
    }
}
