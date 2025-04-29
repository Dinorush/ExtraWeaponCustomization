namespace EWC.CustomWeapon.Properties
{
    public sealed class PropertyRef
    {
        public WeaponPropertyBase Property;
        public uint RefCount = 0;

        public PropertyRef(WeaponPropertyBase prop) => Property = prop;
    }
}
