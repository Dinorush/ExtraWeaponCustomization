using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponFireRateContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public float FireRate { get; set; }

        public WeaponFireRateContext(BulletWeapon weapon, float fireRate)
        {
            Weapon = weapon;
            FireRate = fireRate;
        }
    }
}
