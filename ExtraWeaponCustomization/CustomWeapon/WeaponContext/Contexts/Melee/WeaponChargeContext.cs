using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredWeaponType: Enums.WeaponType.Melee)]
    public sealed class WeaponChargeContext : IWeaponContext
    {
        public float Exponent { get; set; } = 3;

        public WeaponChargeContext() { }
    }
}
