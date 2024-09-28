using System;
using UnityEngine;
using ExtraWeaponCustomization.Utils;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitEnemyContext : WeaponPreHitContext
    {
        public float Damage { get; }
        public float Backstab { get; }
        public new IDamageable Damageable => base.Damageable!;

        public WeaponPreHitEnemyContext(float damage, float falloff, float backstab, IDamageable damageable,
                                        Vector3 position, Vector3 direction, DamageType flag = DamageType.Any) 
            : base(position, direction, falloff, damageable)
        {
            Damage = damage;
            Dam_EnemyDamageBase? baseDam = damageable.GetBaseDamagable()?.TryCast<Dam_EnemyDamageBase>();
            if (baseDam != null)
                Damage = Math.Min(Damage, baseDam.HealthMax);
            Backstab = backstab;
            DamageType = flag;
        }

        public WeaponPreHitEnemyContext(HitData data, float backstab, Dam_EnemyDamageLimb limb, DamageType flag = DamageType.Any)
            : base(data)
        {
            Backstab = backstab;
            Damage = data.damage * Falloff;
            Damage = limb.ApplyWeakspotAndArmorModifiers(Damage, data.precisionMulti);
            Damage *= Backstab;
            Damage = Math.Min(Damage, limb.m_base.HealthMax);
            if (limb.DestructionType == eLimbDestructionType.Custom)
                Damage = Math.Min(Damage, limb.m_healthMax);

            DamageType = flag;
            if (limb.m_type == eLimbDamageType.Weakspot)
                DamageType |= DamageType.Weakspot;
        }
    }
}
