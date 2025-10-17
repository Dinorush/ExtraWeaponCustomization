using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Managed | Enums.OwnerType.Player, requiredWeaponType:Enums.WeaponType.Gun)]
    public sealed class WeaponPostReloadContext : WeaponTriggerContext
    {
        public WeaponPostReloadContext() : base() {}
    }
}
