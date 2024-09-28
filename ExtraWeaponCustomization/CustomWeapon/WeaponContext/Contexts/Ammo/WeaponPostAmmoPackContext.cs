using Player;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPostAmmoPackContext : IWeaponContext
    {
        public PlayerAmmoStorage AmmoStorage { get; set; }

        public WeaponPostAmmoPackContext(PlayerAmmoStorage ammoStorage)
        {
            AmmoStorage = ammoStorage;
        }
    }
}
