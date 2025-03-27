using System;
using EWC.Utils;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitDamageableContext : WeaponHitDamageableContextBase
    {
        public float Damage { get; }
        public float DamageClamped { get; }

        public WeaponHitDamageableContext(float damage, WeaponPreHitDamageableContext context)
            : base(context)
        {
            Damage = damage;
            Dam_SyncedDamageBase? baseDam = Damageable.GetBaseDamagable().TryCast<Dam_SyncedDamageBase>();
            if (baseDam != null)
                DamageClamped = Math.Min(Damage, baseDam.HealthMax);
            else
                DamageClamped = Math.Min(Damage, DamageableUtil.LockHealth);
        }

        public WeaponHitDamageableContext(HitData data)
            : base(data, 1f)
        {
            Damage = data.damage * Falloff;
            var damBase = Damageable.GetBaseDamagable().TryCast<Dam_SyncedDamageBase>();
            if (damBase != null)
                DamageClamped = Math.Min(Damage, damBase.HealthMax);
            else
                DamageClamped = Math.Min(Damage, DamageableUtil.LockHealth);
        }

        public WeaponHitDamageableContext(HitData data, bool bypassTumor, float backstab, Dam_EnemyDamageLimb limb)
            : base(data, backstab)
        {
            Damage = data.damage * Falloff;
            Damage = limb.ApplyWeakspotAndArmorModifiers(Damage, data.precisionMulti);
            // Need to set this AFTER calling the above function again since the patch will clear the cache.
            Patches.Enemy.EnemyLimbPatches.CachedBypassTumorCap = bypassTumor;
            Damage *= Backstab;
            if (!bypassTumor && limb.DestructionType == eLimbDestructionType.Custom)
                Damage = Math.Min(Damage, limb.m_healthMax);
            DamageClamped = Math.Min(Damage, limb.m_base.HealthMax);
        }
    }
}
