using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitDamageableContext : WeaponHitContext
    {
        public new IDamageable Damageable => base.Damageable!;

        public WeaponPreHitDamageableContext(IDamageable damageable, Vector3 position, Vector3 direction, float falloff, DamageType flag = DamageType.Any) : 
            base(position, direction, falloff, damageable)
        {
            DamageType = flag.WithSubTypes(damageable);
        }

        public WeaponPreHitDamageableContext(HitData data, DamageType flag = DamageType.Any) :
            base(data)
        {
            DamageType = flag.WithSubTypes(data.damageable!);
        }
    }
}
