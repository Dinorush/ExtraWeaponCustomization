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
        private readonly static HitData s_hitData = new(DamageType.Bullet);

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
        private static bool PreRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, ref bool __result)
        {
            // Double cast filter
            if (_cachedData == weaponRayData.Pointer) return true;
            _cachedData = weaponRayData.Pointer;

            var cgc = ShotManager.ActiveFiringInfo.cgc;
            if (cgc == null) return true;

            s_hitData.Setup(weaponRayData);
            cgc.Invoke(new WeaponPreRayContext(s_hitData, originPos));
            float mod = cgc.SpreadController.Value;
            if (mod != 1f)
            {
                s_hitData.randomSpread *= mod;
                s_hitData.angOffsetX *= mod;
                s_hitData.angOffsetY *= mod;
            }

            cgc.ShotComponent.FireVanilla(s_hitData, originPos);
            s_hitData.Apply();
            __result = false;
            return false;
        }
    }
}
