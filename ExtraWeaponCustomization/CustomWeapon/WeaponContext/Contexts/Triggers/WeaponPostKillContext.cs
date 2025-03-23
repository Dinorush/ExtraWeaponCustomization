using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponHitDamageableContextBase
    {
        public EnemyAgent Enemy { get; }
        public float Delay { get; }
        public bool DidKill { get; }

        public WeaponPostKillContext(WeaponHitDamageableContext hitContext, float lastTime, bool didKill) :
            base(
                hitContext.Damageable,
                hitContext.LocalPosition + hitContext.Damageable.GetBaseAgent().Position,
                hitContext.Direction,
                hitContext.Normal,
                hitContext.Backstab,
                hitContext.Falloff,
                hitContext.ShotInfo,
                hitContext.DamageType
                )
        {
            Enemy = hitContext.Damageable.GetBaseAgent().Cast<EnemyAgent>();
            Delay = Clock.Time - lastTime;
            DidKill = didKill;
        }
    }
}
