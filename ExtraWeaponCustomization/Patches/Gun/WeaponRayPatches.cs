using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using HarmonyLib;
using static Weapon;
using UnityEngine;
using System.Reflection;
using System;
using EWC.Utils;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.CustomShot;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class WeaponRayPatches
    {
        private static HitData s_hitData = new(DamageType.Bullet);

        [HarmonyTargetMethod]
        private static MethodBase FindWeaponRayFunc(Harmony harmony)
        {
            return AccessTools.Method(
                typeof(Weapon),
                nameof(Weapon.CastWeaponRay),
                new Type[] { typeof(Transform), typeof(Weapon.WeaponHitData).MakeByRefType(), typeof(Vector3), typeof(int) }
                );
        }

        private static IntPtr _cachedData = IntPtr.Zero;

        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool PreRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask, ref bool __result)
        {
            // Sentry filter
            if (altRayCastMask != -1 || _cachedData == weaponRayData.Pointer) return true;
            _cachedData = weaponRayData.Pointer;

            if (ShotManager.FiringWeapon == null) return true;

            var cwc = ShotManager.FiringWeapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return true;

            s_hitData.Setup(weaponRayData, cwc);
            cwc.Invoke(new WeaponPreRayContext(s_hitData, originPos));
            float mod = cwc.SpreadController!.Value;
            if (mod != 1f)
            {
                s_hitData.randomSpread *= mod;
                s_hitData.angOffsetX *= mod;
                s_hitData.angOffsetY *= mod;
            }

            cwc.ShotComponent!.FireVanilla(s_hitData, originPos);
            s_hitData.Apply();
            __result = false;
            return false;
        }
    }
}
