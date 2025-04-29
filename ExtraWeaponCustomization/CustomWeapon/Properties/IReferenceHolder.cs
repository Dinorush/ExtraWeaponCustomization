namespace EWC.CustomWeapon.Properties
{
    public interface IReferenceHolder : IWeaponProperty
    {
        public PropertyList Properties { get; }
        public void OnReferenceSet(WeaponPropertyBase property);
    }
}
