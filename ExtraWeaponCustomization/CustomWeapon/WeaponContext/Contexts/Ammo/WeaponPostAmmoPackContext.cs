using EWC.CustomWeapon.WeaponContext.Attributes;
using Player;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Managed, validWeaponType: Enums.WeaponType.Gun | Enums.WeaponType.SentryHolder)]
    public sealed class WeaponPostAmmoPackContext : IWeaponContext
    {
        public PlayerAmmoStorage AmmoStorage { get; set; }

        public WeaponPostAmmoPackContext(PlayerAmmoStorage ammoStorage)
        {
            AmmoStorage = ammoStorage;
        }
    }
}
