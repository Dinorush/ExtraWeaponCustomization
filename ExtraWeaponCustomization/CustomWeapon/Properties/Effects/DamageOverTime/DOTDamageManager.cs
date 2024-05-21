using Agents;
using CharacterDestruction;
using Enemies;
using ExtraWeaponCustomization.CustomWeapon.KillTracker;
using Player;
using SNetwork;
using System;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public static class DOTDamageManager
    {
        internal static DOTDamageSync Sync { get; private set; } = new();
        public const float MaxStagger = 16384; // 2 ^ 14

        internal static void Init()
        {
            Sync.Setup();
        }

        public static void DoDOTDamage(IDamageable damageable, float damage, DamageOverTime dotBase)
        {
            Dam_EnemyDamageLimb? limb = damageable.TryCast<Dam_EnemyDamageLimb>();
            if (limb == null || limb.m_base.IsImortal || damage <= 0) return;

            damage += 0.001f; // Account for rounding errors
            DOTData data = default;
            data.target.Set(limb.m_base.Owner);
            data.source.Set(dotBase.Owner);
            data.limbID = (byte) (dotBase.DamageLimb ? limb.m_limbID : 0);
            data.localPosition.Set(limb.DamageTargetPos - limb.m_base.Owner.Position, 10f);
            data.staggerMult.Set(dotBase.StaggerMult, MaxStagger);

            bool precHit = !limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot;
            float armorMulti = dotBase.IgnoreArmor ? 1f : limb.m_armorDamageMulti;
            float weakspotMulti = precHit ? Math.Max(limb.m_weakspotDamageMulti * dotBase.PrecisionMult, 1f) : 1f;
            float precDamage = damage * weakspotMulti * armorMulti;

            // Clamp damage for bubbles
            if (limb.TryCast<Dam_EnemyDamageLimb_Custom>() != null)
                precDamage = Math.Min(precDamage, limb.m_healthMax + 1);

            data.damage.Set(precDamage, limb.m_base.DamageMax);

            if (dotBase.Owner != null && dotBase.Owner.IsLocallyOwned == true)
            {
                limb.ShowHitIndicator(precDamage > damage, limb.m_base.WillDamageKill(precDamage), limb.DamageTargetPos, armorMulti < 1f);
                KillTrackerManager.RegisterHit(limb.GetBaseAgent(), limb.DamageTargetPos - limb.m_base.Owner.Position, dotBase.Weapon, precHit);
            }

            Sync.Send(data, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveDOTDamage(EnemyAgent target, PlayerAgent? source, int limbID, Vector3 localPos, float damage, float staggerMult)
        {
            Dam_EnemyDamageBase damBase = target.Damage;
            Dam_EnemyDamageLimb? limb = limbID > 0 ? damBase.DamageLimbs[limbID] : null;
            if (!target.Alive || damBase.IsImortal) return;

            // DoT should only stagger if threshold is reached. Need the hitreact to not be None for the threshold to do anything, though.
            bool staggers = (damage * staggerMult + damBase.m_damBuildToHitreact) >= target.EnemyBalancingData.Health.DamageUntilHitreact;
            ES_HitreactType hitreact = staggers ? ES_HitreactType.Light : ES_HitreactType.None;
            bool tryForceHitreact = false;
            bool willKill = damBase.WillDamageKill(damage);
            CD_DestructionSeverity severity;
            if (willKill)
            {
                tryForceHitreact = true;
                hitreact = ES_HitreactType.Heavy;
                severity = CD_DestructionSeverity.Death;
            }
            else
            {
                severity = CD_DestructionSeverity.Severe;
            }

            Vector3 direction = target.TargetLookDir * -1;
            Vector3 position = localPos + target.Position;
            if (limb != null && (willKill || limb.DoDamage(damage)))
                damBase.CheckDestruction(limb, ref localPos, ref direction, limbID, ref severity, ref tryForceHitreact, ref hitreact);

            damBase.ProcessReceivedDamage(damage, source, position, direction, hitreact, tryForceHitreact, limbID, staggerMult);
        }
    }

    public struct DOTData
    {
        public pEnemyAgent target;
        public pPlayerAgent source;
        public byte limbID;
        public LowResVector3 localPosition;
        public UFloat16 damage;
        public UFloat16 staggerMult;
    }
}
