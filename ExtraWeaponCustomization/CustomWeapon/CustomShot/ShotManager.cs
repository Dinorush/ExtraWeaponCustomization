using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using GameData;
using Gear;
using Player;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.CustomShot
{
    public static class ShotManager
    {
        public struct FiringInfo
        {
            public PlayerAgent? owner = null;
            public InventorySlot slot = InventorySlot.None;
            public WeaponType type = WeaponType.BulletWeapon;
            public bool isTagged = false;
            public ArchetypeDataBlock? archBlock = null;
            public CustomGunComponent? cgc = null;
            public bool valid = false;

            public FiringInfo() { }
        }

        public static uint CurrentID { get; private set; } = 0;
        public static uint NextID => ++CurrentID;
        public static ShotInfoMod CurrentGroupMod { get; private set; } = new();
        // Modifiers applied after EWC systems run. Should not modify direct damage, but should modify EWC damage.
        public static float CurrentExternalDamageMod { get; private set; } = 1f;
        // Modifiers applied to base damage (e.g. boosters). Should modify direct and EWC damage.
        public static float CurrentDamageMod { get; private set; } = 1f;
        public static float CurrentStaggerMod { get; private set; } = 1f;
        public static FiringInfo ActiveFiringInfo { get; private set; }
        public static Vector3 VanillaFireDir { get; set; }

        private static float s_lastShotTime = 0f;
        private static WeaponType s_lastWeaponType = WeaponType.Any;
        private static IntPtr s_lastGroupOwner = IntPtr.Zero;

        private static (IntPtr ptr, ShotInfo info) s_vanillaShotInfo = (IntPtr.Zero, new ShotInfo());
        public static ShotInfo GetVanillaShotInfo(Weapon.WeaponHitData vanillaData)
        {
            if (vanillaData.Pointer != s_vanillaShotInfo.ptr)
            {
                if (ActiveFiringInfo.cgc != null)
                {
                    s_vanillaShotInfo = (vanillaData.Pointer, new ShotInfo(ActiveFiringInfo.cgc, ActiveFiringInfo.isTagged));
                }
                else if (ActiveFiringInfo.valid)
                {
                    AdvanceGroupModIfOld(ActiveFiringInfo.archBlock, ActiveFiringInfo.owner!, ActiveFiringInfo.type, ActiveFiringInfo.slot, ActiveFiringInfo.isTagged);
                    s_vanillaShotInfo = (vanillaData.Pointer, new ShotInfo(vanillaData.damage, vanillaData.precisionMulti, vanillaData.staggerMulti, ActiveFiringInfo.owner!.IsLocallyOwned, ActiveFiringInfo.type));
                }

                if (ActiveFiringInfo.valid && vanillaData.owner == null)
                {
                    // Shotguns only assign these AFTER CastWeaponRay runs, which breaks a lot of logic that rely on them being set.
                    var archData = ActiveFiringInfo.archBlock;
                    vanillaData.owner = ActiveFiringInfo.owner;
                    vanillaData.damage = s_vanillaShotInfo.info.OrigDamage;
                    vanillaData.staggerMulti = s_vanillaShotInfo.info.OrigStagger;
                    vanillaData.precisionMulti = archData!.PrecisionDamageMulti;
                    vanillaData.damageFalloff = archData.DamageFalloff;
                }
            }
            return s_vanillaShotInfo.info;
        }

        public static void CacheFiringGun(BulletWeapon weapon)
        {
            ActiveFiringInfo = new()
            {
                owner = weapon.Owner,
                slot = weapon.AmmoType.ToInventorySlot(),
                type = WeaponType.BulletWeapon,
                archBlock = weapon.ArchetypeData,
                isTagged = false,
                cgc = weapon.GetComponent<CustomGunComponent>(),
                valid = weapon.ArchetypeData != null
            };
        }

        public static void CacheFiringSentry(SentryGunInstance sentry, bool isTagged)
        {
            ActiveFiringInfo = new()
            {
                owner = sentry.Owner,
                slot = InventorySlot.GearClass,
                type = WeaponType.Sentry,
                archBlock = sentry.ArchetypeData,
                isTagged = isTagged,
                cgc = sentry.GetComponent<CustomGunComponent>(),
                valid = sentry.ArchetypeData != null
            };
        }

        public static void ClearFiringInfo()
        {
            ActiveFiringInfo = default;
        }

        public static bool BulletHit(IGunComp weapon, HitData data)
        {
            var hitData = data.Apply(weapon.VanillaHitData);
            s_vanillaShotInfo = (hitData.Pointer, data.shotInfo);

            var oldInfo = ActiveFiringInfo;

            if (weapon.IsType(WeaponType.BulletWeapon))
                CacheFiringGun(((IBulletWeaponComp)weapon).BulletWeapon);
            else // Tagged set to false because the damage is already calculated
                CacheFiringSentry(((SentryGunComp)weapon).Value, isTagged: false);
            var result = BulletWeapon.BulletHit(hitData, true);
            ActiveFiringInfo = oldInfo;
            return result;
        }

        public static void CancelTracerFX(CustomGunComponent cgc)
        {
            int shots = 1;
            if (cgc.Gun.IsShotgun)
                shots = cgc.Gun.ArchetypeData.ShotgunBulletCount;

            var pool = cgc.Gun.IsType(WeaponType.BulletWeapon) ? BulletWeapon.s_tracerPool : SentryGunInstance_Firing_Bullets.s_tracerPool;

            for (int i = 0; i < shots; i++)
            {
                if (!pool.m_inUse[^1].TryCastOut<FX_Effect>(out var effect)) return; // JFS - Shouldn't happen

                foreach (var link in effect.m_links)
                    link.Cast<FX_EffectLink>().m_playingEffect?.ReturnToPool();

                effect.ReturnToPool();
            }
        }

        public static void AdvanceGroupMod(CustomWeaponComponent cwc, bool isTagged = false)
        {
            ArchetypeDataBlock? archBlock = cwc.Weapon is IArchComp archComp ? archComp.ArchetypeData : null;
            AdvanceGroupMod(archBlock, cwc.Owner.Player, cwc.Weapon.Type, cwc.Weapon.InventorySlot, isTagged);
            cwc.Invoke(new WeaponShotGroupInitContext(CurrentGroupMod));
        }
        public static void AdvanceGroupMod(ArchetypeDataBlock? archBlock, PlayerAgent owner, WeaponType weaponType, InventorySlot slot, bool isTagged)
        {
            s_lastGroupOwner = owner.Pointer;
            s_lastShotTime = Clock.Time;
            s_lastWeaponType = weaponType;

            CurrentGroupMod = new();
            if (weaponType.HasFlag(WeaponType.Sentry))
            {
                CurrentExternalDamageMod = 1f;
                CurrentDamageMod = AgentModifierManager.ApplyModifier(owner, AgentModifier.SentryGunDamage, 1f);
                if (isTagged)
                {
                    CurrentDamageMod *= archBlock!.Sentry_DamageTagMulti;
                    CurrentStaggerMod *= archBlock.Sentry_StaggerDamageTagMulti;
                }
            }
            else
            {
                if (weaponType.HasFlag(WeaponType.BulletWeapon))
                {
                    var modifier = slot == InventorySlot.GearStandard ? AgentModifier.StandardWeaponDamage : AgentModifier.SpecialWeaponDamage;
                    CurrentExternalDamageMod = EXPAPIWrapper.GetDamageMod(owner.IsLocallyOwned, weaponType);
                    CurrentDamageMod = AgentModifierManager.ApplyModifier(owner, modifier, 1f);
                }
                else if (weaponType.HasFlag(WeaponType.Melee))
                {
                    var syringeBuff = owner.MeleeBuffTimer > Clock.Time ? 3f : 1f;
                    CurrentExternalDamageMod = EXPAPIWrapper.GetDamageMod(true, weaponType) * syringeBuff;
                    CurrentDamageMod = 1f;
                }
                else
                {
                    CurrentExternalDamageMod = 1f;
                    CurrentDamageMod = 1f;
                }
                CurrentStaggerMod = 1f;
            }
        }

        public static void AdvanceGroupModIfOld(CustomWeaponComponent cwc, bool isTagged)
        {
            float time = Clock.Time;
            if (cwc.Weapon.Type == s_lastWeaponType && time == s_lastShotTime && s_lastGroupOwner == cwc.Owner.Player.Pointer) return;

            AdvanceGroupMod(cwc, isTagged);
        }
        public static void AdvanceGroupModIfOld(ArchetypeDataBlock? archBlock, PlayerAgent owner, WeaponType weaponType, InventorySlot slot, bool isTagged)
        {
            float time = Clock.Time;
            if (weaponType == s_lastWeaponType && time == s_lastShotTime && s_lastGroupOwner == owner.Pointer) return;

            AdvanceGroupMod(archBlock, owner, weaponType, slot, isTagged);
        }
    }
}
