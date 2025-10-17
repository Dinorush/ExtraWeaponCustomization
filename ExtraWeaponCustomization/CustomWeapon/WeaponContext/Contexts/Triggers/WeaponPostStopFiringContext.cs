using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Managed, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponPostStopFiringContext : WeaponTriggerContext
    {
        public WeaponPostStopFiringContext()
        {
        }
    }
}
