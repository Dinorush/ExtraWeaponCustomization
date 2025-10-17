using EWC.CustomWeapon.WeaponContext.Contexts.Base;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostStaggerContext : WeaponHitTrackerContextBase
    {
        public bool LimbBreak { get; }

        public WeaponPostStaggerContext(WeaponHitDamageableContext hitContext, float lastTime, bool lastHit, bool limbBreak) :
            base(hitContext, lastTime, lastHit)
        {
            LimbBreak = limbBreak;
        }
    }
}
