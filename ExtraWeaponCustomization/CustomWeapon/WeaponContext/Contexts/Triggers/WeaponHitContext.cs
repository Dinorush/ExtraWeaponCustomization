using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitContext : WeaponHitContextBase
    {
        public WeaponHitContext(Collider collider, Vector3 position, Vector3 direction, Vector3 normal, float falloff, ShotInfo.Const info) :
            base(collider, position, direction, normal, falloff, info, Enums.DamageType.Bullet | Enums.DamageType.Terrain) {}

        public WeaponHitContext(HitData data) :
            this(data.collider, data.hitPos, data.fireDir.normalized, data.RayHit.normal, data.falloff, data.shotInfo) {}
    }
}
