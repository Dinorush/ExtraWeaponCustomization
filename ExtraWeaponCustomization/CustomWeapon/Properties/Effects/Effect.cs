namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class Effect : IWeaponProperty
    {
        public bool AllowStack { get; } = true;
    }
}
