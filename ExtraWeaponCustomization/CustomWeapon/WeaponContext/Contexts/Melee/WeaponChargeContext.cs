namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponChargeContext : IWeaponContext
    {
        public float Exponent { get; set; } = 3;

        public WeaponChargeContext() { }
    }
}
