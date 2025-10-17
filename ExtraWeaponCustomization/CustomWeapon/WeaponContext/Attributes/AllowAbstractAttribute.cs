using System;

namespace EWC.CustomWeapon.WeaponContext.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    internal sealed class AllowAbstractAttribute : Attribute
    {
        public AllowAbstractAttribute() { }
    }
}
