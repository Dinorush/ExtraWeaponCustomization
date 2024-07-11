namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public abstract class Trait : IWeaponProperty
    {
        public bool AllowStack { get; } = false;
    }
}
