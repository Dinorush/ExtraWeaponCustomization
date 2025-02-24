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

        private static (IntPtr ptr, ShotInfo info) s_vanillaShotInfo = (IntPtr.Zero, new ShotInfo());
        public static ShotInfo GetVanillaShotInfo(Weapon.WeaponHitData vanillaData)
        {
            if (vanillaData.Pointer != s_vanillaShotInfo.ptr)
            {
                s_vanillaShotInfo = (vanillaData.Pointer, new ShotInfo());
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
    }
}
