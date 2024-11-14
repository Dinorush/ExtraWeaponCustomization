using System;
using UnityEngine;
using EWC.Utils;
using EWC.CustomWeapon.Properties.Effects.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitDamageableContext : WeaponPreHitContext
    {
        public float Damage { get; }
        public float Backstab { get; }
        public new IDamageable Damageable => base.Damageable!;

        public WeaponPreHitDamageableContext(float damage, float falloff, float backstab, IDamageable damageable,
                                        Vector3 position, Vector3 direction, DamageType flag = DamageType.Any) 
            : base(position, direction, falloff, damageable)
        {
            Damage = damage;
            Dam_SyncedDamageBase? baseDam = damageable.GetBaseDamagable()?.TryCast<Dam_SyncedDamageBase>();
            if (baseDam != null)
                Damage = Math.Min(Damage, baseDam.HealthMax);
            else
                Damage = Math.Min(Damage, DamageableUtil.LockHealth);
            Backstab = backstab;
            DamageType = flag.WithSubTypes(damageable);
        }

        public WeaponPreHitDamageableContext(HitData data, IDamageable damageable, DamageType flag = DamageType.Any)
            : base(data)
        {
            Damage = data.damage * Falloff;
            var damBase = damageable.GetBaseDamagable()?.TryCast<Dam_SyncedDamageBase>();
            if (damBase != null)
                Damage = Math.Min(Damage, damBase.HealthMax);
            else
                Damage = Math.Min(Damage, DamageableUtil.LockHealth);
            DamageType = flag.WithSubTypes(damageable);
            Backstab = 1f;
        }

        public WeaponPreHitDamageableContext(HitData data, bool bypassTumor, float backstab, Dam_EnemyDamageLimb limb, DamageType flag = DamageType.Any)
            : base(data)
        {
            Backstab = Math.Max(backstab, 1f);
            Damage = data.damage * Falloff;
            Damage = limb.ApplyWeakspotAndArmorModifiers(Damage, data.precisionMulti);
            Damage *= Backstab;
            Damage = Math.Min(Damage, limb.m_base.HealthMax);
            if (!bypassTumor && limb.DestructionType == eLimbDestructionType.Custom)
                Damage = Math.Min(Damage, limb.m_healthMax);

            DamageType = flag.WithSubTypes(limb);
        }
    }
}
