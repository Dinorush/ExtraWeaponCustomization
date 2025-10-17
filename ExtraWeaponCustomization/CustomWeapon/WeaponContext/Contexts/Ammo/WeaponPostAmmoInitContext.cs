using EWC.CustomWeapon.WeaponContext.Attributes;
using Player;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Managed, validWeaponType: Enums.WeaponType.Gun | Enums.WeaponType.SentryHolder)]
    public sealed class WeaponPostAmmoInitContext : IWeaponContext
    {
        public PlayerAmmoStorage AmmoStorage { get; set; }
        public InventorySlotAmmo SlotAmmo { get; set; }

        public WeaponPostAmmoInitContext(PlayerAmmoStorage ammoStorage, InventorySlotAmmo slotAmmo)
        {
            AmmoStorage = ammoStorage;
            SlotAmmo = slotAmmo;
        }
    }
}
