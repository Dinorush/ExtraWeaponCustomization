﻿using ExtraWeaponCustomization.Utils;
using System;
using Gear;
using static Weapon;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitEnemyContext : WeaponPreHitContext
    {
        public float Damage { get; }
        public float Backstab { get; }
        public new IDamageable Damageable => base.Damageable!;

        public WeaponPreHitEnemyContext(float damage, float falloff, float backstab, IDamageable damageable,
                                        Vector3 position, Vector3 direction, BulletWeapon weapon, DamageFlag flag = DamageFlag.Any) 
            : base(position, direction, falloff, weapon, damageable)
        {
            Damage = damage;
            Dam_EnemyDamageBase? baseDam = damageable.GetBaseDamagable()?.TryCast<Dam_EnemyDamageBase>();
            if (baseDam != null)
                Damage = Math.Min(Damage, baseDam.HealthMax);
            Backstab = backstab;
            DamageFlag = flag;
        }

        public WeaponPreHitEnemyContext(WeaponHitData data, float additionalDist, Dam_EnemyDamageLimb limb, BulletWeapon weapon, DamageFlag flag = DamageFlag.Any)
            : base(data, additionalDist, weapon, limb.TryCast<IDamageable>())
        {
            Backstab = limb.ApplyDamageFromBehindBonus(1f, data.rayHit.point, data.fireDir.normalized);

            Damage = data.damage * Falloff;
            Damage = limb.ApplyWeakspotAndArmorModifiers(Damage, data.precisionMulti);
            Damage *= Backstab;
            Damage = Math.Min(Damage, limb.m_base.HealthMax);
            if (limb.DestructionType == eLimbDestructionType.Custom)
                Damage = Math.Min(Damage, limb.m_healthMax);

            DamageFlag = flag;
            if (limb.m_type == eLimbDamageType.Weakspot)
                DamageFlag |= DamageFlag.Weakspot;
        }
    }
}
