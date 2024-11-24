using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponHitContext : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float Falloff { get; }
        public Vector3 LocalPosition { get; }
        public IDamageable? Damageable { get; }

        public WeaponHitContext(Vector3 position, Vector3 direction, float falloff, IDamageable? damageable = null) : base(DamageType.Bullet)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
            LocalPosition = position - damageable?.GetBaseAgent()?.Position ?? Vector3.zero;
            Damageable = damageable;
        }

        public WeaponHitContext(HitData data) :
            this(data.hitPos, data.fireDir.normalized, data.falloff, data.damageable) {}
    }
}
