using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponDamageContext : IWeaponContext
    {
        public float Damage { get; set; }
        public IDamageable Damageable { get; }
        public BulletWeapon Weapon { get; }

        public WeaponDamageContext(float damage, IDamageable damageable, BulletWeapon weapon)
        {
            Damage = damage;
            Damageable = damageable;
            Weapon = weapon;
        }
    }
}
