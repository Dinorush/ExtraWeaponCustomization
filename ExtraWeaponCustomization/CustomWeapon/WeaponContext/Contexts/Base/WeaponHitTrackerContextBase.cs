using Enemies;

namespace EWC.CustomWeapon.WeaponContext.Contexts.Base
{
    public abstract class WeaponHitTrackerContextBase : WeaponHitDamageableContextBase
    {
        public EnemyAgent Enemy { get; }
        public float Delay { get; }
        public bool DidLastHit { get; }

        public WeaponHitTrackerContextBase(WeaponHitDamageableContext hitContext, float lastTime, bool didKill) :
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
            DidLastHit = didKill;
        }
    }
}
