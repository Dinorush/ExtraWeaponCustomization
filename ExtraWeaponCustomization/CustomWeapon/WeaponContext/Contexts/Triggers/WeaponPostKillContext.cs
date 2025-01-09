using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponHitDamageableContextBase
    {
        public EnemyAgent Enemy { get; }

        public WeaponPostKillContext(WeaponHitDamageableContext hitContext) :
            base(
                hitContext.Damageable,
                hitContext.LocalPosition + hitContext.Damageable.GetBaseAgent().Position,
                hitContext.Direction,
                hitContext.Normal,
                hitContext.Backstab,
                hitContext.Falloff,
                hitContext.DamageType
                )
        {
            Enemy = hitContext.Damageable.GetBaseAgent().Cast<EnemyAgent>();
        }
    }
}
