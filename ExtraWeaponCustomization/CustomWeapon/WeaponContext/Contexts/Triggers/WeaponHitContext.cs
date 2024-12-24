using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponHitContext : WeaponHitContextBase
    {
        public Collider Collider { get; }

        public WeaponHitContext(Vector3 position, Vector3 direction, float falloff, Collider collider) :
            base(position, direction, falloff)
        {
            Collider = collider;
        }

        public WeaponHitContext(HitData data) :
            this(data.hitPos, data.fireDir.normalized, data.falloff, data.RayHit.collider) {}
    }
}
