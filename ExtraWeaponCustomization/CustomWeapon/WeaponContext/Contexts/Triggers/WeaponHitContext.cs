using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponHitContext : WeaponHitContextBase
    {
        public IDamageable? Damageable { get; }

        public WeaponHitContext(Vector3 position, Vector3 direction, float falloff, IDamageable? damageable = null) :
            base(position, direction, falloff, damageable)
        {
            Damageable = damageable;
        }

        public WeaponHitContext(HitData data) :
            this(data.hitPos, data.fireDir.normalized, data.falloff, data.damageable) {}
    }
}
