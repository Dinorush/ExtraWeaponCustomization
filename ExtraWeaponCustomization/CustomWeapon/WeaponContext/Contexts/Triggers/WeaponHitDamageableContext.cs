﻿using System;
using EWC.Utils;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.CustomWeapon.Enums;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitDamageableContext : WeaponHitDamageableContextBase
    {
        public float Damage { get; }

        public WeaponHitDamageableContext(float damage, WeaponPreHitDamageableContext context)
            : base(context)
        {
            Damage = damage;
            Dam_SyncedDamageBase? baseDam = Damageable.GetBaseDamagable().TryCast<Dam_SyncedDamageBase>();
            if (baseDam != null)
                Damage = Math.Min(Damage, baseDam.HealthMax);
            else
                Damage = Math.Min(Damage, DamageableUtil.LockHealth);
        }

        public WeaponHitDamageableContext(HitData data, DamageType flag)
            : base(data, 1f, flag)
        {
            Damage = data.damage * Falloff;
            var damBase = Damageable.GetBaseDamagable().TryCast<Dam_SyncedDamageBase>();
            if (damBase != null)
                Damage = Math.Min(Damage, damBase.HealthMax);
            else
                Damage = Math.Min(Damage, DamageableUtil.LockHealth);
        }

        public WeaponHitDamageableContext(HitData data, bool bypassTumor, float backstab, Dam_EnemyDamageLimb limb, DamageType flag)
            : base(data, backstab, flag)
        {
            Damage = data.damage * Falloff;
            Damage = limb.ApplyWeakspotAndArmorModifiers(Damage, data.precisionMulti);
            // Need to set this AFTER calling the above function again since the patch will clear the cache.
            Patches.Enemy.EnemyLimbPatches.CachedBypassTumorCap = bypassTumor;
            Damage *= Backstab;
            Damage = Math.Min(Damage, limb.m_base.HealthMax);
            if (!bypassTumor && limb.DestructionType == eLimbDestructionType.Custom)
                Damage = Math.Min(Damage, limb.m_healthMax);
        }
    }
}
