﻿using Agents;
using AIGraph;
using CharacterDestruction;
using Enemies;
using EWC.API;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using EWC.Utils.Extensions;
using EWC.Utils.Log;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.Explosion
{
    public static class ExplosionManager
    {
        private readonly static ExplosionDamageSync _sync = new();

        private const SearchSetting BaseSettings = SearchSetting.CheckLOS | SearchSetting.CacheHit;
        private static SearchSetting s_searchSetting = BaseSettings;
        public const float MaxRadius = 1024f;
        public const float MaxStagger = 16384f; // 2^14

        private readonly static List<RaycastHit> s_hits = new();
        private readonly static ShotInfo s_shotInfo = new();

        internal static void Init()
        {
            _sync.Setup();
            ExplosionFXManager.Init();
        }

        public static void DoExplosion(Vector3 position, Vector3 direction, PlayerAgent source, float falloffMod, Explosive eBase, float triggerAmt)
        {
            if (!source.IsLocallyOwned) return;

            ExplosionFXManager.DoExplosionFX(position, eBase);
            DoExplosionDamage(position, direction, source, falloffMod, eBase, triggerAmt);
        }

        internal static void DoExplosionDamage(Vector3 position, Vector3 direction, PlayerAgent source, float falloffMod, Explosive explosiveBase, float triggerAmt)
        {
            AIG_CourseNode? node = SearchUtil.GetCourseNode(position, source);
            if (node == null)
            {
                EWCLogger.Error($"Unable to find node containing position [{position}] for an explosion.");
                return;
            }

            Ray ray = new(position, direction);
            s_searchSetting = BaseSettings;
            if (explosiveBase.DamageOwner)
                s_searchSetting |= SearchSetting.CheckOwner;
            if (explosiveBase.DamageFriendly)
                s_searchSetting |= SearchSetting.CheckFriendly;

            SearchUtil.SightBlockLayer = LayerUtil.MaskWorldExcProj;
            foreach ((_, RaycastHit hit) in SearchUtil.GetEnemyHitsInRange(ray, explosiveBase.Radius, 180f, node, s_searchSetting))
                s_hits.Add(hit);

            if (explosiveBase.DamageFriendly || explosiveBase.DamageOwner)
                foreach ((_, RaycastHit hit) in SearchUtil.GetPlayerHitsInRange(ray, explosiveBase.Radius, 180f, s_searchSetting))
                    s_hits.Add(hit);

            if (explosiveBase.DamageLocks)
                s_hits.AddRange(SearchUtil.GetLockHitsInRange(ray, explosiveBase.Radius, 180f, s_searchSetting));

            foreach (RaycastHit hit in s_hits)
            {
                SendExplosionDamage(
                    hit.collider.GetComponent<IDamageable>(),
                    hit.point,
                    direction,
                    hit.normal,
                    hit.distance,
                    source,
                    falloffMod,
                    s_shotInfo,
                    explosiveBase,
                    triggerAmt);
            }
            s_shotInfo.Reset();
            s_hits.Clear();
        }

        internal static void SendExplosionDamage(IDamageable damageable, Vector3 position, Vector3 direction, Vector3 normal, float distance, PlayerAgent source, float falloffMod, ShotInfo info, Explosive eBase, float triggerAmt)
        {
            float damage = distance.MapInverted(eBase.InnerRadius, eBase.Radius, eBase.MaxDamage, eBase.MinDamage, eBase.Exponent);
            float distFalloff = damage / eBase.MaxDamage;
            damage *= triggerAmt;
            float precisionMult = eBase.PrecisionDamageMulti;

            float backstabMulti = 1f;
            bool enemy = damageable.IsEnemy();
            Dam_EnemyDamageLimb? limb = null;
            if (enemy)
            {
                limb = damageable.Cast<Dam_EnemyDamageLimb>();
                if (!eBase.IgnoreBackstab)
                {
                    if (eBase.CacheBackstab > 0f)
                        backstabMulti = eBase.CacheBackstab;
                    else
                    {
                        float mod = eBase.CWC.Invoke(new WeaponBackstabContext()).Value;
                        backstabMulti = limb.ApplyDamageFromBehindBonus(1f, position, direction).Map(1f, 2f, 1f, mod);
                    }
                }
            }

            var preContext = eBase.CWC.Invoke(new WeaponPreHitDamageableContext(
                damageable,
                position,
                direction,
                normal,
                backstabMulti,
                falloffMod * distFalloff,
                info,
                DamageType.Explosive
                ));
            info.AddHit(preContext.DamageType);

            WeaponDamageContext damageContext = new(damage, precisionMult, damageable);
            eBase.CWC.Invoke(damageContext);
            if (!eBase.IgnoreDamageMods)
                damage = damageContext.Damage.Value;
            precisionMult = damageContext.Precision.Value;

            // 0.001f to account for rounding error
            damage = falloffMod * damage + 0.001f;
            damage *= EXPAPIWrapper.GetDamageMod(eBase.CWC.IsGun);

            Agent? agent = damageable.GetBaseAgent();
            if (agent?.Type == AgentType.Player)
            {
                if (agent == eBase.CWC.Weapon.Owner)
                {
                    if (!eBase.DamageOwner)
                        return;
                }
                else if (!eBase.DamageFriendly)
                    return;

                GuiManager.CrosshairLayer.PopFriendlyTarget();
                Dam_PlayerDamageBase playerBase = damageable.GetBaseDamagable().Cast<Dam_PlayerDamageBase>();
                damage *= playerBase.m_playerData.friendlyFireMulti * eBase.FriendlyDamageMulti;
                damage *= EXPAPIWrapper.GetExplosionResistanceMod(playerBase.Owner);
                eBase.CWC.Invoke(new WeaponHitDamageableContext(damage, preContext));
                // Only damage and direction are used AFAIK, but again, just in case...
                playerBase.BulletDamage(damage, source, position, playerBase.DamageTargetPos - position, Vector3.zero);
                return;
            }
            else if (agent == null) // Lock damage; direction doesn't matter
            {
                eBase.CWC.Invoke(new WeaponHitDamageableContext(damage, preContext));
                damageable.BulletDamage(damage, source, Vector3.zero, Vector3.zero, Vector3.zero);
                return;
            }

            if (!enemy) return;

            Dam_EnemyDamageBase damBase = limb!.m_base;
            Vector3 localPosition = position - damBase.Owner.Position;
            ExplosionDamageData data = default;
            data.target.Set(damBase.Owner);
            data.source.Set(source);
            data.limbID = (byte) limb.m_limbID;
            data.damageLimb = eBase.DamageLimb;
            data.localPosition.Set(localPosition, 10f);
            data.staggerMult.Set(eBase.StaggerDamageMulti, MaxStagger);

            bool precHit = !limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot;
            float armorMulti = eBase.IgnoreArmor ? 1f : limb.m_armorDamageMulti;
            float weakspotMulti = precHit ? Math.Max(limb.m_weakspotDamageMulti * precisionMult, 1f) : 1f;
            
            float precDamage = damage * weakspotMulti * armorMulti * backstabMulti;

            // Clamp damage for bubbles
            if (!damageContext.BypassTumorCap && limb.DestructionType == eLimbDestructionType.Custom)
                precDamage = Math.Min(precDamage, limb.m_healthMax + 1);

            data.damage.Set(precDamage, damBase.DamageMax);

            var hitContext = eBase.CWC.Invoke(new WeaponHitDamageableContext(precDamage, preContext));

            bool willKill = damBase.WillDamageKill(precDamage);
            KillTrackerManager.RegisterHit(eBase.CWC.Weapon, hitContext);
            if (willKill || eBase.CWC.Invoke(new WeaponHitmarkerContext(damBase.Owner)).Result)
                limb.ShowHitIndicator(precDamage > damage, willKill, position, armorMulti < 1f || damBase.IsImortal);

            _sync.Send(data, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveExplosionDamage(EnemyAgent target, PlayerAgent? source, int limbID, bool damageLimb, Vector3 localPos, float damage, float staggerMult)
        {
            Dam_EnemyDamageBase damBase = target.Damage;
            Dam_EnemyDamageLimb limb = damBase.DamageLimbs[limbID];
            if (damBase.Health <= 0 || !damBase.Owner.Alive || damBase.IsImortal) return;

            DamageAPI.FirePreExplosiveCallbacks(damage, target, limb, source);

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
            if (damageLimb && (willKill || limb.DoDamage(damage)))
                damBase.CheckDestruction(limb, ref localPos, ref direction, limbID, ref severity, ref tryForceHitreact, ref hitreact);

            Vector3 position = localPos + target.Position;
            damBase.ProcessReceivedDamage(damage, source, position, Vector3.up * 1000f, hitreact, tryForceHitreact, limbID, staggerMult);
            DamageAPI.FirePostExplosiveCallbacks(damage, target, limb, source);
        }
    }

    public struct ExplosionDamageData
    {
        public pEnemyAgent target;
        public pPlayerAgent source;
        public byte limbID;
        public bool damageLimb;
        public LowResVector3 localPosition;
        public UFloat16 damage;
        public UFloat16 staggerMult;
    }
}