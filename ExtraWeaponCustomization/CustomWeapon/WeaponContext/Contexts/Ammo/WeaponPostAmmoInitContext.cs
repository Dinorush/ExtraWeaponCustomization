using Gear;
using Player;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPostAmmoInitContext : IWeaponContext
    {
        public PlayerAmmoStorage AmmoStorage { get; set; }
        public InventorySlotAmmo SlotAmmo { get; set; }
        public BulletWeapon Weapon { get; }

        public WeaponPostAmmoInitContext(PlayerAmmoStorage ammoStorage, InventorySlotAmmo slotAmmo, BulletWeapon weapon)
        {
            AmmoStorage = ammoStorage;
            SlotAmmo = slotAmmo;
            Weapon = weapon;
        }
    }
}
