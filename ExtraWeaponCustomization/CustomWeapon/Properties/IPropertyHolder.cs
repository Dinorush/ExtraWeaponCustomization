namespace EWC.CustomWeapon.Properties
{
    public interface IPropertyHolder : IWeaponProperty
    {
        public PropertyList Properties { get; }
        public PropertyNode Node { get; set; }
    }
}
