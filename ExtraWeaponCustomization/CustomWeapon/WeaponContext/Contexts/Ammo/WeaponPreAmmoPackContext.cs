using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreAmmoPackContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public float AmmoRel { get; set; }

        public WeaponPreAmmoPackContext(BulletWeapon weapon, float ammoRel)
        {
            Weapon = weapon;
            AmmoRel = ammoRel;
        }
    }
}
