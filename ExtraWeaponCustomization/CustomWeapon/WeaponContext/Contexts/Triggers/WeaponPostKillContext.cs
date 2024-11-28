using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponHitContextBase
    {
        public EnemyAgent Enemy { get; }
        public float Backstab { get; }

        public WeaponPostKillContext(WeaponHitDamageableContext hitContext) :
            base(
                hitContext.LocalPosition + hitContext.Damageable.GetBaseAgent().Cast<EnemyAgent>().Position,
                hitContext.LocalPosition,
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
