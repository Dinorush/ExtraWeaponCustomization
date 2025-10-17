using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.Utils;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitContext : WeaponHitContextBase
    {
        public WeaponHitContext(HitData data) :
            base(data.gameObject!, data.hitPos, data.fireDir.normalized, data.RayHit.normal, data.falloff, data.shotInfo, data.damageType) {}
    }
}
