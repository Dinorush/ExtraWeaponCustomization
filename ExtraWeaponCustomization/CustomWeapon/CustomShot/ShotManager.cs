using EWC.Utils;
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
    }
}
