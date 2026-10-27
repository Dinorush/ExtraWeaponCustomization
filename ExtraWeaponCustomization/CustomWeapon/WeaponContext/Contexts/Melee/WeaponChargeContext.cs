using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local, requiredWeaponType: Enums.WeaponType.Melee)]
    public sealed class WeaponChargeContext : IWeaponContext
    {
        public float Exponent { get; set; } = 3;

        public WeaponChargeContext() { }
    }
}
