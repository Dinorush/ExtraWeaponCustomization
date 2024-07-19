using ExtraWeaponCustomization.Utils;
using Gear;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    // HACK - This context doesn't use DamageFlag, but it is more convenient for HitEnemy to inherit from this (for Explosive)
    public class WeaponPreHitContext : WeaponDamageFlagContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float Falloff { get; }
        public Vector3 LocalPosition { get; }
        public IDamageable? Damageable { get; }

        public WeaponPreHitContext(Vector3 position, Vector3 direction, float falloff, BulletWeapon weapon, IDamageable? damageable = null) : base(weapon, DamageFlag.Invalid)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
            LocalPosition = position - damageable?.GetBaseAgent().Position ?? Vector3.zero;
            Damageable = damageable;
            DamageFlag = DamageFlag.Invalid;
        }

        public WeaponPreHitContext(WeaponHitData data, float additionalDist, BulletWeapon weapon, IDamageable? damageable = null) :
            this(data.rayHit.point, data.fireDir.normalized, data.Falloff(additionalDist), weapon, damageable) {}
    }
}
