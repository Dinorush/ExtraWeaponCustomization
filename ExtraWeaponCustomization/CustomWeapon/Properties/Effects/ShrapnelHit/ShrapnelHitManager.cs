using Agents;
using CharacterDestruction;
using Enemies;
using EWC.API;
using EWC.Attributes;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.HitTracker;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Structs;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using EWC.Utils.Extensions;
using Player;
using SNetwork;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.ShrapnelHit
{
    public static class ShrapnelHitManager
    {
        private readonly static ShrapnelHitSync _sync = new();

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
        }

        public static bool DoHit(Shrapnel shrapnel, HitData hitData, ContextController cc, bool calcFalloff = true)
        {
            if (hitData.damage <= 0) return true;

            if (hitData.damageable == null || hitData.damageType.HasFlag(DamageType.Dead))
            {
                cc.Invoke(new WeaponHitContext(hitData));
                return true;
            }

            float damage = hitData.damage; // Account for rounding errors
            float precisionMult = hitData.precisionMulti;
            float staggerMult = hitData.staggerMulti;
            float falloff = hitData.falloff;
            if (calcFalloff)
                falloff *= hitData.CalcFalloff();

            var damageable = hitData.damageable;
            float backstabMulti = 1f;
            float origBackstabMulti = 1f;
            bool enemy = hitData.damageType.HasFlag(DamageType.Enemy);
            Dam_EnemyDamageLimb? limb = null;
            if (enemy)
            {
                limb = damageable.Cast<Dam_EnemyDamageLimb>();
                if (!shrapnel.IgnoreBackstab && shrapnel.CWC.Weapon.AllowBackstab)
                {
                    float mod = cc.Invoke(new WeaponBackstabContext()).Value;
                    origBackstabMulti = limb.ApplyDamageFromBehindBonus(1f, hitData.hitPos, hitData.RayHit.point - shrapnel.CWC.Owner.FirePos);
                    backstabMulti = origBackstabMulti.Map(1f, 2f, 1f, mod);
                }
            }

            var oldFalloff = hitData.falloff;
            hitData.falloff = falloff;
            var preContext = new WeaponPreHitDamageableContext(hitData, backstabMulti, origBackstabMulti);
            hitData.falloff = oldFalloff;
            cc.Invoke(preContext);

            WeaponStatContext statContext = new(hitData, shrapnel.CWC.DebuffIDs);
            cc.Invoke(statContext);
            if (!shrapnel.IgnoreShotMods)
            {
                damage = statContext.Damage;
                precisionMult = statContext.Precision;
                staggerMult = statContext.Stagger;
            }

            if (damage <= 0) return true;

            hitData.shotInfo.AddHit(preContext.DamageType);

            // 0.001f to account for rounding error
            damage = damage * falloff + 0.001f;
            damage *= hitData.shotInfo.ExternalDamageMod * hitData.shotInfo.InnateDamageMod;
            staggerMult *= hitData.shotInfo.InnateStaggerMod;

            PlayerAgent? source = shrapnel.CWC.Owner.Player;
            Agent? agent = damageable.GetBaseAgent();
            if (hitData.damageType.HasFlag(DamageType.Player))
            {
                if (agent == source)
                {
                    if (!shrapnel.DamageOwner)
                        return false;
                }
                else if (!shrapnel.DamageFriendly)
                    return false;

                if (source?.IsLocallyOwned == true)
                    GuiManager.CrosshairLayer.PopFriendlyTarget();
                Dam_PlayerDamageBase playerBase = damageable.GetBaseDamagable().Cast<Dam_PlayerDamageBase>();
                damage *= playerBase.m_playerData.friendlyFireMulti * shrapnel.FriendlyDamageMulti;
                cc.Invoke(new WeaponHitDamageableContext(damage, preContext));
                // Only damage and direction are used AFAIK, but again, just in case...
                playerBase.BulletDamage(damage, source, hitData.hitPos, hitData.fireDir, hitData.RayHit.normal);
                return true;
            }
            else if (hitData.damageType.HasFlag(DamageType.Lock)) // Lock damage; direction doesn't matter
            {
                if (!shrapnel.DamageLocks)
                    return false;
                cc.Invoke(new WeaponHitDamageableContext(damage, preContext));
                damageable.BulletDamage(damage, source, hitData.hitPos, hitData.fireDir, hitData.RayHit.normal);
                return true;
            }

            if (!enemy) return false;

            Dam_EnemyDamageBase damBase = limb!.m_base;
            Vector3 localPosition = hitData.hitPos - damBase.Owner.Position;
            ShrapnelHitData data = default;
            data.target.Set(damBase.Owner);
            data.cwc.Set(shrapnel.CWC);
            data.limbID = (byte)limb.m_limbID;
            data.damageLimb = shrapnel.DamageLimb;
            data.localPosition.Set(localPosition, 10f);
            data.dir.Value = hitData.fireDir;
            data.staggerMult = staggerMult;
            data.setCooldowns = shrapnel.ApplyAttackCooldown;

            bool precHit = limb.m_type == eLimbDamageType.Weakspot;
            float armorMulti = shrapnel.IgnoreArmor ? 1f : limb.m_armorDamageMulti;
            DebuffManager.GetAndApplyArmorShredDebuff(ref armorMulti, damageable, shrapnel.CWC.DebuffIDs);
            float weakspotMulti = precHit ? Math.Max(limb.m_weakspotDamageMulti * precisionMult, 1f) : 1f;

            float precDamage = damage * weakspotMulti * armorMulti * backstabMulti;

            // Clamp damage for bubbles
            if (!statContext.BypassTumorCap && limb.DestructionType == eLimbDestructionType.Custom)
                precDamage = Math.Min(precDamage, limb.m_healthMax + 1);

            data.damage = precDamage;

            var hitContext = cc.Invoke(new WeaponHitDamageableContext(precDamage, preContext));

            bool willKill = damBase.WillDamageKill(precDamage);
            HitTrackerManager.RegisterHit(shrapnel.CWC.Owner, shrapnel.CWC, hitContext);
            if (source?.IsLocallyOwned == true && (willKill || cc.Invoke(new WeaponHitmarkerContext(damBase.Owner)).Result))
                limb.ShowHitIndicator(precDamage > damage, willKill, hitData.hitPos, armorMulti < 1f || damBase.IsImortal);

            _sync.Send(data, SNet_ChannelType.GameNonCritical);
            return true;
        }

        internal static void Internal_ReceiveShrapnelDamage(EnemyAgent target, PlayerAgent? source, OwnerType ownerType, int limbID, bool damageLimb, Vector3 localPos, Vector3 direction, float damage, float staggerMult, bool setCooldowns)
        {
            Dam_EnemyDamageBase damBase = target.Damage;
            Dam_EnemyDamageLimb limb = damBase.DamageLimbs[limbID];
            if (damBase.Health <= 0 || !damBase.Owner.Alive || damBase.IsImortal) return;

            DamageAPI.FirePreShrapnelCallbacks(damage, target, limb, source, ownerType);

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

            if (damageLimb && (willKill || limb.DoDamage(damage)))
                damBase.CheckDestruction(limb, ref localPos, ref direction, limbID, ref severity, ref tryForceHitreact, ref hitreact);

            Vector3 position = localPos + target.Position;
            ProcessReceivedShrapnelDamage(damBase, damage, source, position, direction, hitreact, tryForceHitreact, staggerMult, setCooldowns);
            DamageSyncWrapper.RunDamageSync(target, damageLimb ? limbID : -1);
            DamageAPI.FirePostShrapnelCallbacks(damage, target, limb, source, ownerType);
        }

        private static void ProcessReceivedShrapnelDamage(Dam_EnemyDamageBase damBase, float damage, Agent? damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact = false, float staggerDamageMulti = 1f, bool setCooldowns = true)
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

    public struct ShrapnelHitData
    {
        public pEnemyAgent target;
        public pCWC cwc;
        public byte limbID;
        public bool damageLimb;
        public LowResVector3 localPosition;
        public LowResVector3_Normalized dir;
        public float damage;
        public float staggerMult;
        public bool setCooldowns;
    }
}
