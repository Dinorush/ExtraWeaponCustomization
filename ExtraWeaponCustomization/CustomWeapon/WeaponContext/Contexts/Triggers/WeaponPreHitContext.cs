using ExtraWeaponCustomization.Utils;
using Gear;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreHitContext : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float Falloff { get; }
        public Vector3 LocalPosition { get; }
        public IDamageable? Damageable { get; }

        public WeaponPreHitContext(Vector3 position, Vector3 direction, float falloff, BulletWeapon weapon, IDamageable? damageable = null) : base(weapon, DamageType.Bullet)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
            LocalPosition = position - damageable?.GetBaseAgent().Position ?? Vector3.zero;
            Damageable = damageable;
        }

        public WeaponPreHitContext(WeaponHitData data, float additionalDist, BulletWeapon weapon, IDamageable? damageable = null) :
            this(data.rayHit.point, data.fireDir.normalized, data.Falloff(additionalDist), weapon, damageable) {}
    }
}
