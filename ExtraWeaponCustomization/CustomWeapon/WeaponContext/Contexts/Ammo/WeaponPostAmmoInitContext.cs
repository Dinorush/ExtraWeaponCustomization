using Player;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPostAmmoInitContext : IWeaponContext
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
