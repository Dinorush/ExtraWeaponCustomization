using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitContext : WeaponHitContextBase
    {
        public WeaponHitContext(Collider collider, Vector3 position, Vector3 direction, Vector3 normal, float falloff, bool hitCorpse, ShotInfo.Const info) :
            base(collider, position, direction, normal, falloff, info, hitCorpse ? Enums.DamageType.Bullet : Enums.DamageType.Bullet | Enums.DamageType.Terrain) { }

        public WeaponHitContext(HitData data, bool hitCorpse) :
            this(data.collider, data.hitPos, data.fireDir.normalized, data.RayHit.normal, data.falloff, hitCorpse, data.shotInfo) {}
    }
}
