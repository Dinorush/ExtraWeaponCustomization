using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Managed, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponPostStartFireContext : WeaponTriggerContext
    {
        public WeaponPostStartFireContext()
        {
        }
    }
}
