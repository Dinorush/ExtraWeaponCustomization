namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponFireRateModContextSync : IWeaponContext
    {
        public float Mod { get; }

        public WeaponFireRateModContextSync(float mod)
        {
            Mod = mod;
        }
    }
}
