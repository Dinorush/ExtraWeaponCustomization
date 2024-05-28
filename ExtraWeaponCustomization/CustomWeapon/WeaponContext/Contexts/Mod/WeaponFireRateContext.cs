using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponFireRateContext : WeaponStackModContext
    {
        public WeaponFireRateContext(float fireRate, BulletWeapon weapon) : base(fireRate, weapon) { }
    }
}
