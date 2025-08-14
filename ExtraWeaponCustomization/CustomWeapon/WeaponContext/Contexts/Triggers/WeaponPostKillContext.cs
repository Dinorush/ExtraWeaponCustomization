using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponHitTrackerContextBase
    {
        public WeaponPostKillContext(WeaponHitDamageableContext hitContext, float lastTime, bool lastHit) :
            base(hitContext, lastTime, lastHit) {}
    }
}
