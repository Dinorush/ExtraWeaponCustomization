using ExtraWeaponCustomization.Utils;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreHitContext : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float Falloff { get; }
        public Vector3 LocalPosition { get; }
        public IDamageable? Damageable { get; }

        public WeaponPreHitContext(Vector3 position, Vector3 direction, float falloff, IDamageable? damageable = null) : base(DamageType.Bullet)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
            LocalPosition = position - damageable?.GetBaseAgent()?.Position ?? Vector3.zero;
            Damageable = damageable;
        }

        public WeaponPreHitContext(HitData data) :
            this(data.hitPos, data.fireDir.normalized, data.falloff, data.damageable) {}
    }
}
