using Gear;
using Player;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPostAmmoPackContext : IWeaponContext
    {
        public PlayerAmmoStorage AmmoStorage { get; set; }
        public BulletWeapon Weapon { get; }

        public WeaponPostAmmoPackContext(PlayerAmmoStorage ammoStorage, BulletWeapon weapon)
        {
            AmmoStorage = ammoStorage;
            Weapon = weapon;
        }
    }
}
