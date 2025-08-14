using Agents;
using AIGraph;
using AK;
using CharacterDestruction;
using Enemies;
using EWC.API;
using EWC.Attributes;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.HitTracker;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using EWC.Utils.Extensions;
using GameEvent;
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
        private readonly static ExplosionDamagePlayerSync _playerSync = new();

        private const SearchSetting BaseSettings = SearchSetting.CheckLOS | SearchSetting.ClosestHit;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
            _playerSync.Setup();
        }

        public static void DoExplosion(Vector3 position, Vector3 direction, Vector3 normal, PlayerAgent source, float falloffMod, Explosive eBase, float triggerAmt, ShotInfo? triggerInfo = null)
        {
            if (!source.IsLocallyOwned) return;

            ExplosionFXManager.DoExplosionFX(position, normal, eBase);
            DoExplosionDamage(position, direction, source, falloffMod, eBase, triggerAmt, triggerInfo);
        }

        internal static void DoExplosionDamage(Vector3 position, Vector3 direction, PlayerAgent source, float falloffMod, Explosive explosiveBase, float triggerAmt, ShotInfo? triggerInfo)
        {
            if (explosiveBase.Radius == 0 || (explosiveBase.MaxDamage == 0 && explosiveBase.MinDamage == 0)) return;

            AIG_CourseNode? node = CourseNodeUtil.GetCourseNode(position, source.DimensionIndex);
            if (node == null)
            {
                EWCLogger.Error($"Unable to find node containing position [{position}] for an explosion.");
                return;
            }

            Ray ray = new(position, direction);
            SearchSetting searchSetting = BaseSettings;
            if (explosiveBase.DamageOwner)
                searchSetting |= SearchSetting.CheckOwner;
            if (explosiveBase.DamageFriendly)
                searchSetting |= SearchSetting.CheckFriendly;

            List<RaycastHit> hits = new();
            SearchUtil.SightBlockLayer = LayerUtil.MaskWorldExcProj;
            foreach ((_, RaycastHit hit) in SearchUtil.GetEnemyHitsInRange(ray, explosiveBase.Radius, 180f, node, searchSetting))
                hits.Add(hit);

            if (explosiveBase.DamageFriendly || explosiveBase.DamageOwner)
                foreach ((_, RaycastHit hit) in SearchUtil.GetPlayerHitsInRange(ray, explosiveBase.Radius, 180f, searchSetting))
                    hits.Add(hit);

            if (explosiveBase.DamageLocks)
                hits.AddRange(SearchUtil.GetLockHitsInRange(ray, explosiveBase.Radius, 180f, searchSetting));

            if (explosiveBase.HitClosestFirst)
                hits.Sort(SortUtil.Rayhit);

            ShotInfo shotInfo;
            if (triggerInfo != null)
                shotInfo = new(triggerInfo, true, explosiveBase.UseParentShotMod);
            else
            {
                shotInfo = new();
                shotInfo.NewShot(explosiveBase.CWC);
            }

            var oldInfo = shotInfo.State;
            foreach (RaycastHit hit in hits)
            {
                SendExplosionDamage(
                    hit.collider.GetComponent<IDamageable>(),
                    hit.point,
                    direction,
                    hit.normal,
                    hit.distance,
                    source,
                    falloffMod,
                    shotInfo,
                    explosiveBase,
                    triggerAmt);
            }
            explosiveBase.CWC.Invoke(new WeaponShotEndContext(DamageType.Explosive, shotInfo.State, oldInfo));
        }

        internal static void SendExplosionDamage(IDamageable damageable, Vector3 position, Vector3 direction, Vector3 normal, float distance, PlayerAgent source, float falloffMod, ShotInfo info, Explosive eBase, float triggerAmt)
        {
            float damage = distance.MapInverted(eBase.InnerRadius, eBase.Radius, eBase.MaxDamage, eBase.MinDamage, eBase.Exponent);
            float distFalloff = damage / eBase.MaxDamage;
            damage *= triggerAmt;
            float precisionMult = eBase.PrecisionDamageMulti;
            float staggerMult = eBase.StaggerDamageMulti;

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

            if (damage <= 0) return;

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

            WeaponStatContext statContext = new(damage, precisionMult, staggerMult, preContext.DamageType, damageable, info, eBase.CWC.DebuffIDs);
            eBase.CWC.Invoke(statContext);
            if (!eBase.IgnoreShotMods)
            {
                damage = statContext.Damage;
                precisionMult = statContext.Precision;
                staggerMult = statContext.Stagger;
            }

            if (damage <= 0) return;

            info.AddHit(preContext.DamageType);

            // 0.001f to account for rounding error
            damage = falloffMod * damage + 0.001f;
            damage *= info.XpMod;

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
                if (damage == 0) return;

                ExplosionDamagePlayerData playerData = default;
                playerData.target.Set(playerBase.Owner);
                playerData.source.Set(source);
                playerData.damage = damage;
                playerData.direction.Value = direction;

                GuiManager.CrosshairLayer.PopFriendlyTarget();
                _playerSync.Send(playerData);
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
            data.staggerMult = staggerMult;

            bool precHit = !limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot;
            float armorMulti = eBase.IgnoreArmor ? 1f : limb.m_armorDamageMulti;
            float weakspotMulti = precHit ? Math.Max(limb.m_weakspotDamageMulti * precisionMult, 1f) : 1f;
            
            float precDamage = damage * weakspotMulti * armorMulti * backstabMulti;

            // Clamp damage for bubbles
            if (!statContext.BypassTumorCap && limb.DestructionType == eLimbDestructionType.Custom)
                precDamage = Math.Min(precDamage, limb.m_healthMax + 1);

            data.damage = precDamage;

            var hitContext = eBase.CWC.Invoke(new WeaponHitDamageableContext(precDamage, preContext));

            bool willKill = damBase.WillDamageKill(precDamage);
            HitTrackerManager.RegisterHit(eBase.CWC, hitContext);
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

        internal static void Internal_ReceiveExplosionDamagePlayer(PlayerAgent target, PlayerAgent? source, float damage, Vector3 direction)
        {
            var damBase = target.Damage;
            if (target.IsLocallyOwned)
            {
                target.Sound.Post(EVENTS.BULLETHITPLAYERSYNC);
                if (source == null || source.Pointer != target.Pointer)
                {
                    PlayerDialogManager.WantToStartDialog(152u, target.CharacterID);
                    GameEventManager.PostEvent(eGameEvent.player_take_friendly_fire, target, damage);
                }
                damBase.Cast<Dam_PlayerDamageLocal>().Hitreact(damage, direction);
            }
            damBase.OnIncomingDamage(damage, damage, source);
        }

    }

    public struct ExplosionDamageData
    {
        public pEnemyAgent target;
        public pPlayerAgent source;
        public byte limbID;
        public bool damageLimb;
        public LowResVector3 localPosition;
        public float damage;
        public float staggerMult;
    }

    public struct ExplosionDamagePlayerData
    {
        public pPlayerAgent target;
        public pPlayerAgent source;
        public float damage;
        public LowResVector3_Normalized direction;
    }
}