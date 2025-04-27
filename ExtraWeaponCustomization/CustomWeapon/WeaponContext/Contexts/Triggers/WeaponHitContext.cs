using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitContext : WeaponHitContextBase
    {
        public WeaponHitContext(HitData data) :
            base(data.collider, data.hitPos, data.fireDir.normalized, data.RayHit.normal, data.falloff, data.shotInfo, data.damageType) {}
    }
}
