using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponHitDamageableContextBase
    {
        public EnemyAgent Enemy { get; }
        public float Backstab { get; }

        public WeaponPostKillContext(WeaponHitDamageableContext hitContext) :
            base(
                hitContext.Damageable,
                hitContext.LocalPosition + hitContext.Damageable.GetBaseAgent().Position,
                hitContext.Direction,
                hitContext.Falloff
                )
        {
            Enemy = hitContext.Damageable.GetBaseAgent().Cast<EnemyAgent>();
            Backstab = hitContext.Backstab;
            DamageType = hitContext.DamageType;
        }
    }
}
