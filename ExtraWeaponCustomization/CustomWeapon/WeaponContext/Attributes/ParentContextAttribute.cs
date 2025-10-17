using System;

namespace EWC.CustomWeapon.WeaponContext.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    internal sealed class ParentContextAttribute : Attribute
    {
        private Type _parent;
        public Type Parent
        { 
            get => _parent;
            set { _parent = value; ValidateType(); }
        }

        public ParentContextAttribute(Type contextType)
        {
            _parent = contextType;
            ValidateType();
        }

        private void ValidateType()
        {
            if (!typeof(IWeaponContext).IsAssignableFrom(_parent))
                throw new ArgumentException("Parent Context attribute requires an IWeaponContext type.");
        }
    }
}
