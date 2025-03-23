using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitDamageableContext : WeaponHitDamageableContextBase
    {
        public WeaponPreHitDamageableContext(IDamageable damageable, Vector3 position, Vector3 direction, Vector3 normal, float backstab, float falloff, ShotInfo.Const info, DamageType flag) : 
            base(damageable, position, direction, normal, backstab, falloff, info, flag) {}

        public WeaponPreHitDamageableContext(HitData data, float backstab) :
            base(data, backstab) {}
    }
}
