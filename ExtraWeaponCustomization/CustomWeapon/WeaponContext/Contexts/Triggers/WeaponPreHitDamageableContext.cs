using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitDamageableContext : WeaponHitDamageableContextBase
    {
        public WeaponPreHitDamageableContext(IDamageable damageable, Vector3 position, Vector3 direction, float backstab, float falloff, DamageType flag) : 
            base(damageable, position, direction, backstab, falloff, flag) {}

        public WeaponPreHitDamageableContext(HitData data, float backstab, DamageType flag) :
            base(data, backstab, flag) {}
    }
}
