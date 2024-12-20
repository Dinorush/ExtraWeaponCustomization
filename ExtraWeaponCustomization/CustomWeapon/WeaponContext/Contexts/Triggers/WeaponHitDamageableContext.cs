﻿using System;
using EWC.Utils;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitDamageableContext : WeaponHitDamageableContextBase
    {
        public float Damage { get; }
        public float Backstab { get; }

        public WeaponHitDamageableContext(float damage, float backstab, WeaponPreHitDamageableContext context)
            : base(context.Damageable, context.Position, context.Direction, context.Falloff)
        {
            Damage = damage;
            Dam_SyncedDamageBase? baseDam = Damageable.GetBaseDamagable().TryCast<Dam_SyncedDamageBase>();
            if (baseDam != null)
                Damage = Math.Min(Damage, baseDam.HealthMax);
            else
                Damage = Math.Min(Damage, DamageableUtil.LockHealth);
            Backstab = backstab;
            DamageType = context.DamageType;
        }

        public WeaponHitDamageableContext(HitData data, DamageType flag = DamageType.Any)
            : base(data)
        {
            Damage = data.damage * Falloff;
            var damBase = Damageable.GetBaseDamagable().TryCast<Dam_SyncedDamageBase>();
            if (damBase != null)
                Damage = Math.Min(Damage, damBase.HealthMax);
            else
                Damage = Math.Min(Damage, DamageableUtil.LockHealth);
            DamageType = flag.WithSubTypes(data.damageable!);
            Backstab = 1f;
        }

        public WeaponHitDamageableContext(HitData data, bool bypassTumor, float backstab, Dam_EnemyDamageLimb limb, DamageType flag = DamageType.Any)
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
