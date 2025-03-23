using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using FX_EffectSystem;
using Gear;
using System;

namespace EWC.CustomWeapon.CustomShot
{
    public static class ShotManager
    {
        public static uint CurrentID { get; private set; } = 0;
        public static uint NextID => ++CurrentID;
        public static ShotInfoMod CurrentGroupMod { get; private set; } = new();
        public static Shotgun? CachedShotgun { get; set; }

        private static float s_lastShotTime = 0f;

        private static (IntPtr ptr, ShotInfo info) s_vanillaShotInfo = (IntPtr.Zero, new ShotInfo());
        public static ShotInfo GetVanillaShotInfo(Weapon.WeaponHitData vanillaData, CustomWeaponComponent cwc)
        {
            if (vanillaData.Pointer != s_vanillaShotInfo.ptr)
            {
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

                s_vanillaShotInfo = (vanillaData.Pointer, new ShotInfo(vanillaData.damage, vanillaData.precisionMulti, vanillaData.staggerMulti));
                s_vanillaShotInfo.info.GroupMod = CurrentGroupMod;
                cwc.Invoke(new WeaponShotInitContext(s_vanillaShotInfo.info.Mod));
            }
            return s_vanillaShotInfo.info;
        }

        public static bool BulletHit(HitData data)
        {
            var hitData = data.Apply(Weapon.s_weaponRayData);
            s_vanillaShotInfo = (hitData.Pointer, data.shotInfo);
            return BulletWeapon.BulletHit(hitData, true);
        }

        public static void CancelTracerFX(GameData.ArchetypeDataBlock archData, bool isShotgun)
        {
            int shots = 1;
            if (isShotgun)
                shots = archData.ShotgunBulletCount;

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

            CurrentGroupMod = new();
            cwc.Invoke(new WeaponShotGroupInitContext(CurrentGroupMod));
            s_lastShotTime = time;
        }
    }
}
