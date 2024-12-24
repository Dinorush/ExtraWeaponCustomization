using Agents;
using CharacterDestruction;
using Enemies;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using Player;
using SNetwork;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT
{
    public static class DOTDamageManager
    {
        private readonly static DOTEnemyDamageSync _sync = new();
        public const float MaxStagger = 16384; // 2 ^ 14

        internal static void Init()
        {
            _sync.Setup();
            DOTGlowPooling.Initialize();
        }

        internal static void Reset()
        {
            DOTGlowPooling.Reset();
        }

        public static void DoDOTDamage(IDamageable damageable, float damage, float falloff, float precisionMulti, bool bypassTumor, float backstabMulti, DamageOverTime dotBase)
        {
            if (!dotBase.Owner.IsLocallyOwned || damage <= 0) return;

            damage = damage * falloff + 0.001f; // Account for rounding errors            

            Agent? agent = damageable.GetBaseAgent();
            if (agent?.Type == AgentType.Player)
            {
                Dam_PlayerDamageBase playerBase = damageable.GetBaseDamagable().Cast<Dam_PlayerDamageBase>();
                damage *= playerBase.m_playerData.friendlyFireMulti * dotBase.FriendlyDamageMulti;
                var prePlayerContext = dotBase.CWC.Invoke(new WeaponPreHitDamageableContext(
                    damageable,
                    damageable.DamageTargetPos,
                    damageable.DamageTargetPos - playerBase.Owner.Position,
                    falloff,
                    DamageType.DOT
                    ));
                dotBase.CWC.Invoke(new WeaponHitDamageableContext(damage, 1f, prePlayerContext));
                // Don't really need custom damage behavior, but BulletDamage triggers FF dialogue.
                SendPlayerDOTDamage(damage, playerBase, dotBase.Owner);
                return;
            }
            else if (agent == null) // Lock damage; direction and damage function don't matter
            {
                var preLockContext = dotBase.CWC.Invoke(new WeaponPreHitDamageableContext(
                    damageable,
                    damageable.DamageTargetPos,
                    Vector3.up,
                    falloff,
                    DamageType.DOT
                    ));
                dotBase.CWC.Invoke(new WeaponHitDamageableContext(damage, 1f, preLockContext));
                damageable.BulletDamage(damage, dotBase.Owner, Vector3.zero, Vector3.zero, Vector3.zero);
                return;
            }

            Dam_EnemyDamageLimb? limb = damageable.TryCast<Dam_EnemyDamageLimb>();
            if (limb == null) return;

            Dam_EnemyDamageBase damBase = limb.m_base;
            DOTData data = default;
            data.target.Set(damBase.Owner);
            data.source.Set(dotBase.Owner);
            data.limbID = (byte)(dotBase.DamageLimb ? limb.m_limbID : 0);
            data.localPosition.Set(limb.DamageTargetPos - damBase.Owner.Position, 10f);
            data.staggerMult.Set(dotBase.StaggerDamageMulti, MaxStagger);
            data.setCooldowns = dotBase.ApplyAttackCooldown;

            bool precHit = !limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot;
            float armorMulti = dotBase.IgnoreArmor ? 1f : limb.m_armorDamageMulti;
            float weakspotMulti = precHit ? Math.Max(limb.m_weakspotDamageMulti * precisionMulti, 1f) : 1f;
            float precDamage = damage * weakspotMulti * armorMulti * backstabMulti;

            // Clamp damage for bubbles
            if (!bypassTumor && limb.DestructionType == eLimbDestructionType.Custom)
                precDamage = Math.Min(precDamage, limb.m_healthMax + 1);

            data.damage.Set(precDamage, damBase.DamageMax);

            var preContext = dotBase.CWC.Invoke(new WeaponPreHitDamageableContext(
                damageable,
                limb.DamageTargetPos,
                limb.DamageTargetPos - damBase.Owner.Position,
                falloff,
                DamageType.DOT
                ));
            var hitContext = dotBase.CWC.Invoke(new WeaponHitDamageableContext(precDamage, backstabMulti, preContext));

            KillTrackerManager.RegisterHit(dotBase.CWC.Weapon, hitContext);
            limb.ShowHitIndicator(precDamage > damage, damBase.WillDamageKill(precDamage), hitContext.Position, armorMulti < 1f || damBase.IsImortal);

            _sync.Send(data, SNet_ChannelType.GameNonCritical);
        }

        // Not using damBase.FireDamage to avoid invoking XP's bleed resistance
        private static void SendPlayerDOTDamage(float damage, Dam_PlayerDamageBase damBase, Agent source)
        {
            pSmallDamageData data = default;
            data.damage.Set(damage, damBase.HealthMax);
            data.source.Set(source);
            if (SNet.IsMaster)
                damBase.m_fireDamagePacket.Send(data, SNet_ChannelType.GameNonCritical);
            else
                damBase.m_fireDamagePacket.Send(data, SNet_ChannelType.GameNonCritical, SNet.Master);
            damBase.ReceiveFireDamage(data);
        }

        internal static void Internal_ReceiveDOTEnemyDamage(EnemyAgent target, PlayerAgent? source, int limbID, Vector3 localPos, float damage, float staggerMult, bool setCooldowns)
        {
            Dam_EnemyDamageBase damBase = target.Damage;
            Dam_EnemyDamageLimb? limb = limbID > 0 ? damBase.DamageLimbs[limbID] : null;
            if (!target.Alive || damBase.IsImortal) return;

            // DoT should only stagger if threshold is reached. Need the hitreact to not be None for the threshold to do anything, though.
            bool staggers = damage * staggerMult + damBase.m_damBuildToHitreact >= target.EnemyBalancingData.Health.DamageUntilHitreact;
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

            EXPAPIWrapper.RegisterDamage(target, source, damage, willKill);

            Vector3 direction = target.TargetLookDir * -1;
            Vector3 position = localPos + target.Position;
            if (limb != null && (willKill || limb.DoDamage(damage)))
                damBase.CheckDestruction(limb, ref localPos, ref direction, limbID, ref severity, ref tryForceHitreact, ref hitreact);

            ProcessReceivedDOTDamage(damBase, damage, source, position, direction, hitreact, tryForceHitreact, staggerMult, setCooldowns);
        }

        private static void ProcessReceivedDOTDamage(Dam_EnemyDamageBase damBase, float damage, Agent? damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact = false, float staggerDamageMulti = 1f, bool setCooldowns = true)
        {
            EnemyAgent owner = damBase.Owner;
            bool num = damBase.RegisterDamage(damage);
            owner.RegisterDamageInflictor(damageSource);
            if (setCooldowns)
                owner.Abilities.OnTakeDamage(damage);
            bool flag = false;
            if (num)
            {
                hitreact = ES_HitreactType.ToDeath;
                flag = true;
            }
            else
            {
                damBase.m_damBuildToHitreact += damage * staggerDamageMulti;
                if (tryForceHitreact || damBase.m_damBuildToHitreact >= owner.EnemyBalancingData.Health.DamageUntilHitreact)
                {
                    flag = true;
                    damBase.m_damBuildToHitreact = 0f;
                }
            }
            if (flag && owner.Locomotion.Hitreact.CanHitreact(hitreact, tryForceHitreact))
            {
                ImpactDirection direction2 = ES_Hitreact.GetDirection(owner.transform, direction);
                owner.Locomotion.Hitreact.ActivateState(hitreact, direction2, attackerIsPlayer: true, damageSource, position, DamageNoiseLevel.Normal);
            }
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
        public bool setCooldowns;
    }
}
