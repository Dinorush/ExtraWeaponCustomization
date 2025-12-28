using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredWeaponType: Enums.WeaponType.Melee)]
    internal class WeaponPrePushContext : WeaponTriggerContext
    {
        public WeaponPrePushContext() : base() { }
    }
}
