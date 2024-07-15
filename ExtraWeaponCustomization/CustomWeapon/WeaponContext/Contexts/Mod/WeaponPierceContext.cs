using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPierceContext : WeaponStackModContext
    {
        public IDamageable Damageable { get; }

        public WeaponPierceContext(float damage, IDamageable damageable, BulletWeapon weapon) : base(damage, weapon)
        {
            Damageable = damageable;
        }
    }
}
