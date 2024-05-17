using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPostAmmoPackContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPostAmmoPackContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
