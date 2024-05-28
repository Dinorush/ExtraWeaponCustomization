using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponFireRateSetContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public float FireRate { get; set; }

        public WeaponFireRateSetContext(BulletWeapon weapon, float fireRate)
        {
            Weapon = weapon;
            FireRate = fireRate;
        }
    }
}
