using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitEnemyContext : WeaponTriggerContext
    {
        public float Falloff { get; }
        public float Backstab { get; }
        public IDamageable Damageable { get; }

        public WeaponPreHitEnemyContext(float falloff, float backstab, IDamageable damageable, BulletWeapon weapon, TriggerType type = TriggerType.OnHit) : base(weapon, type)
        {
            Falloff = falloff;
            Backstab = backstab;
            Damageable = damageable;
        }
    }
}
