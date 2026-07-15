using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: OwnerType.Local)]
    public sealed class WeaponChargeSpeedContext : WeaponStackModContext
    {
        public WeaponChargeSpeedContext() : base(1f) { }
    }
}
