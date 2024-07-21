using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponDamageContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public IDamageable Damageable { get; }
        public StackMod Damage { get; }
        public StackMod Precision { get; }

        public WeaponDamageContext(float damage, float precision, IDamageable damageable, BulletWeapon weapon)
        {
            Weapon = weapon;
            Damage = new(damage);
            Precision = new(precision);
            Damageable = damageable;
        }
    }
}
