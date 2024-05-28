using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponDamageContext : WeaponStackModContext
    {
        public IDamageable Damageable { get; }

        public WeaponDamageContext(float damage, IDamageable damageable, BulletWeapon weapon) : base(damage, weapon)
        {
            Damageable = damageable;
        }
    }
}
