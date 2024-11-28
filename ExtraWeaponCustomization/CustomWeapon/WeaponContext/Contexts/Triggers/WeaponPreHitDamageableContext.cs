using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitDamageableContext : WeaponHitDamageableContextBase
    {
        public WeaponPreHitDamageableContext(IDamageable damageable, Vector3 position, Vector3 direction, float falloff, DamageType flag = DamageType.Any) : 
            base(damageable, position, direction, falloff)
        {
            DamageType = flag.WithSubTypes(damageable);
        }

        public WeaponPreHitDamageableContext(HitData data, DamageType flag = DamageType.Any) :
            base(data)
        {
            DamageType = flag.WithSubTypes(Damageable);
        }
    }
}
