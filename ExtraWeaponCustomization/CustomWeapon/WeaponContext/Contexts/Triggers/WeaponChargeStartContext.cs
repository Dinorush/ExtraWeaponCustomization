using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local)]
    public sealed class WeaponChargeStartContext : WeaponTriggerContext
    {
        public WeaponChargeStartContext() : base() { }
    }
}
