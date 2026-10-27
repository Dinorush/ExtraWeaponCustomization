using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Managed, requiredWeaponType: Enums.WeaponType.Melee)]
    internal class WeaponPostPushContext : WeaponTriggerContext
    {
        public WeaponPostPushContext() : base() { }
    }
}
