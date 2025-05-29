using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using FX_EffectSystem;
using Gear;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.CustomShot
{
    public static class ShotManager
    {
        public static uint CurrentID { get; private set; } = 0;
        public static uint NextID => ++CurrentID;
        public static ShotInfoMod CurrentGroupMod { get; private set; } = new();
        public static Shotgun? CachedShotgun { get; set; }
        public static Vector3 VanillaFireDir { get; set; }

        private static float s_lastShotTime = 0f;

        private static (IntPtr ptr, ShotInfo info) s_vanillaShotInfo = (IntPtr.Zero, new ShotInfo());
        private static bool s_hasRanShotEnd = true;
        public static ShotInfo GetVanillaShotInfo(Weapon.WeaponHitData vanillaData, CustomWeaponComponent cwc)
        {
            if (vanillaData.Pointer != s_vanillaShotInfo.ptr)
            {
                RunVanillaShotEnd(cwc);
                s_hasRanShotEnd = false;

                // Shotguns only assign these AFTER CastWeaponRay runs, which breaks a lot of logic that rely on them being set.
                if (CachedShotgun != null)
                {
                    var archData = CachedShotgun.ArchetypeData;
                    vanillaData.owner = CachedShotgun.Owner;
                    vanillaData.damage = archData.GetDamageWithBoosterEffect(CachedShotgun.Owner, CachedShotgun.ItemDataBlock.inventorySlot);
                    vanillaData.staggerMulti = archData.StaggerDamageMulti;
                    vanillaData.precisionMulti = archData.PrecisionDamageMulti;
                    vanillaData.damageFalloff = archData.DamageFalloff;
                }

                s_vanillaShotInfo = (vanillaData.Pointer, new ShotInfo(vanillaData.damage, vanillaData.precisionMulti, vanillaData.staggerMulti, cwc.IsGun));
                s_vanillaShotInfo.info.GroupMod = CurrentGroupMod;
                cwc.Invoke(new WeaponShotInitContext(s_vanillaShotInfo.info.Mod));
            }
            return s_vanillaShotInfo.info;
        }

        public static void CancelHandleShotEnd() => s_hasRanShotEnd = true;
        public static void RunVanillaShotEnd(CustomWeaponComponent cwc)
        {
            if (s_hasRanShotEnd) return;

            s_hasRanShotEnd = true;
            cwc.Invoke(new WeaponShotEndContext(Enums.DamageType.Bullet, s_vanillaShotInfo.info, null));
        }

        public static bool BulletHit(HitData data)
        {
            var hitData = data.Apply(Weapon.s_weaponRayData);
            s_vanillaShotInfo = (hitData.Pointer, data.shotInfo);
            return BulletWeapon.BulletHit(hitData, true);
        }

        public static void CancelTracerFX(CustomWeaponComponent cwc)
        {
            int shots = 1;
            if (cwc.IsShotgun)
                shots = cwc.ArchetypeData.ShotgunBulletCount;

            for (int i = 0; i < shots; i++)
            {
                var effect = BulletWeapon.s_tracerPool.m_inUse[^1].TryCast<FX_Effect>();
                if (effect == null) return; // JFS - Shouldn't happen

                foreach (var link in effect.m_links)
                    link.TryCast<FX_EffectLink>()!.m_playingEffect?.ReturnToPool();

                effect.ReturnToPool();
            }
        }

        public static void AdvanceGroupMod(CustomWeaponComponent cwc)
        {
            CurrentGroupMod = new();
            cwc.Invoke(new WeaponShotGroupInitContext(CurrentGroupMod));
        }

        public static void AdvanceGroupModIfOld(CustomWeaponComponent cwc)
        {
            float time = Clock.Time;
            if (time == s_lastShotTime) return;

            AdvanceGroupMod(cwc);
            s_lastShotTime = time;
        }
    }
}
