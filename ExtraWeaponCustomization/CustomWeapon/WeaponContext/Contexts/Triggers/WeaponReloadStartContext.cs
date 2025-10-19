using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Managed | Enums.OwnerType.Player, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponReloadStartContext : WeaponTriggerContext
    {
        public WeaponReloadStartContext() : base() {}
    }
}
