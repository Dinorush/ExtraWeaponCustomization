using Agents;
using AIGraph;
using CharacterDestruction;
using Enemies;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using EWC.Utils.Log;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public static class ExplosionManager
    {
        internal static ExplosionDamageSync DamageSync { get; private set; } = new();

        private const SearchSetting SearchSettings = SearchSetting.CheckLOS | SearchSetting.CacheHit;
        public const float MaxRadius = 1024f;
        public const float MaxStagger = 16384f; // 2^14

        private readonly static List<RaycastHit> s_hits = new();

        internal static void Init()
        {
            DamageSync.Setup();
            ExplosionFXManager.Init();
        }

        public static void DoExplosion(Vector3 position, Vector3 direction, PlayerAgent source, float falloffMod, Explosive eBase, float triggerAmt, IDamageable? directLimb = null)
        {
            if (!source.IsLocallyOwned) return;

            ExplosionFXManager.DoExplosionFX(position, eBase);
            DoExplosionDamage(position, direction, source, falloffMod, eBase, triggerAmt, directLimb);
        }

        internal static void DoExplosionDamage(Vector3 position, Vector3 direction, PlayerAgent source, float falloffMod, Explosive explosiveBase, float triggerAmt, IDamageable? directLimb = null)
        {
            AIG_CourseNode? node = SearchUtil.GetCourseNode(position, source);
            if (node == null)
            {
                EWCLogger.Error($"Unable to find node containing position [{position}] for an explosion.");
                return;
            }

            Ray ray = new(position, direction);
            SearchUtil.SightBlockLayer = LayerManager.MASK_EXPLOSION_BLOCKERS;
            foreach ((_, RaycastHit hit) in SearchUtil.GetEnemyHitsInRange(ray, explosiveBase.Radius, 180f, node, SearchSettings))
                s_hits.Add(hit);

            foreach ((_, RaycastHit hit) in SearchUtil.GetPlayerHitsInRange(ray, explosiveBase.Radius, 180f, SearchSettings))
                s_hits.Add(hit);

            s_hits.AddRange(SearchUtil.GetLockHitsInRange(ray, explosiveBase.Radius, 180f, SearchSettings));

            foreach (RaycastHit hit in s_hits)
            {
                SendExplosionDamage(
                    hit.collider.GetComponent<IDamageable>(),
                    hit.point,
                    direction,
                    hit.distance,
                    source,
                    falloffMod,
                    explosiveBase,
                    triggerAmt);
            }
            s_hits.Clear();
        }

        internal static void SendExplosionDamage(IDamageable damageable, Vector3 position, Vector3 direction, float distance, PlayerAgent source, float falloffMod, Explosive eBase, float triggerAmt)
        {
            float damage = distance.Map(eBase.InnerRadius, eBase.Radius, eBase.MaxDamage, eBase.MinDamage);
            float distFalloff = damage / eBase.MaxDamage;
            damage *= triggerAmt;
            float precisionMult = eBase.PrecisionDamageMulti;

            WeaponDamageContext damageContext = new(damage, precisionMult, damageable);
            eBase.CWC.Invoke(damageContext);
            if (!eBase.IgnoreDamageMods)
                damage = damageContext.Damage.Value;
            precisionMult = damageContext.Precision.Value;

            // 0.001f to account for rounding error
            damage = falloffMod * damage + 0.001f;

            Agent? agent = damageable.GetBaseAgent();
            if (agent?.Type == AgentType.Player)
            {
                if (agent == eBase.Weapon.Owner)
                {
                    if (!eBase.DamageOwner)
                        return;
                }
                else if (!eBase.DamageFriendly)
                    return;

                GuiManager.CrosshairLayer.PopFriendlyTarget();
                // Seems like the damageable is always the base, but just in case
                Dam_PlayerDamageBase playerBase = damageable.GetBaseDamagable().TryCast<Dam_PlayerDamageBase>()!;
                damage *= playerBase.m_playerData.friendlyFireMulti;
                // Only damage and direction are used AFAIK, but again, just in case...
                playerBase.BulletDamage(damage, source, position, playerBase.DamageTargetPos - position, Vector3.zero);
                return;
            }
            else if (agent == null) // Lock damage; direction doesn't matter
            {
                damageable.BulletDamage(damage, source, Vector3.zero, Vector3.zero, Vector3.zero);
                return;
            }

            // Applied after FF damage since EXP mod doesn't affect FF damage
            EXPAPIWrapper.ApplyMod(ref damage);

            Dam_EnemyDamageLimb? limb = damageable.TryCast<Dam_EnemyDamageLimb>();
            if (limb == null || limb.m_base.IsImortal) return;

            Vector3 localPosition = position - limb.m_base.Owner.Position;
            ExplosionDamageData data = default;
            data.target.Set(limb.m_base.Owner);
            data.source.Set(source);
            data.limbID = (byte)(eBase.DamageLimb ? limb.m_limbID : 0);
            data.localPosition.Set(localPosition, 10f);
            data.staggerMult.Set(eBase.StaggerDamageMulti, MaxStagger);

            bool precHit = !limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot;
            float armorMulti = eBase.IgnoreArmor ? 1f : limb.m_armorDamageMulti;
            float weakspotMulti = precHit ? Math.Max(limb.m_weakspotDamageMulti * precisionMult, 1f) : 1f;
            float backstabMulti = 1f;
            if (!eBase.IgnoreBackstab)
            {
                if (eBase.CacheBackstab > 0f)
                    backstabMulti = eBase.CacheBackstab;
                else
                {
                    WeaponBackstabContext backContext = new();
                    eBase.CWC.Invoke(backContext);
                    backstabMulti = limb.ApplyDamageFromBehindBonus(1f, position, direction).Map(1f, 2f, 1f, backContext.Value);
                }
            }
            float precDamage = damage * weakspotMulti * armorMulti * backstabMulti;

            // Clamp damage for bubbles
            if (!damageContext.BypassTumorCap && limb.DestructionType == eLimbDestructionType.Custom)
                precDamage = Math.Min(precDamage, limb.m_healthMax + 1);

            data.damage.Set(precDamage, limb.m_base.DamageMax);

            WeaponPreHitEnemyContext hitContext = new(
                precDamage,
                distFalloff * falloffMod,
                backstabMulti,
                damageable,
                position,
                direction,
                precHit ? DamageType.WeakspotExplosive : DamageType.Explosive
                );
            eBase.CWC.Invoke(hitContext);

            KillTrackerManager.RegisterHit(eBase.Weapon, hitContext);
            limb.ShowHitIndicator(precDamage > damage, limb.m_base.WillDamageKill(precDamage), position, armorMulti < 1f);

            DamageSync.Send(data, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveExplosionDamage(EnemyAgent target, PlayerAgent? source, int limbID, Vector3 localPos, float damage, float staggerMult)
        {
            Dam_EnemyDamageBase damBase = target.Damage;
            Dam_EnemyDamageLimb? limb = limbID > 0 ? damBase.DamageLimbs[limbID] : null;
            if (!damBase.Owner.Alive || damBase.IsImortal) return;

            ES_HitreactType hitreact = staggerMult > 0 ? ES_HitreactType.Light : ES_HitreactType.None;
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

            Vector3 direction = Vector3.up;
            if (limb != null && (willKill || limb.DoDamage(damage)))
                damBase.CheckDestruction(limb, ref localPos, ref direction, limbID, ref severity, ref tryForceHitreact, ref hitreact);
            
            Vector3 position = localPos + target.Position;
            damBase.ProcessReceivedDamage(damage, source, position, Vector3.up * 1000f, hitreact, tryForceHitreact, limbID, staggerMult);
        }
    }

    public struct ExplosionDamageData
    {
        public pEnemyAgent target;
        public pPlayerAgent source;
        public byte limbID;
        public LowResVector3 localPosition;
        public UFloat16 damage;
        public UFloat16 staggerMult;
    }
}