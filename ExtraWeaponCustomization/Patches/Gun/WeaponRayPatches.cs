using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using HarmonyLib;
using static Weapon;
using UnityEngine;
using System.Reflection;
using System;
using Gear;
using EWC.Utils;

namespace EWC.Patches
{
    [HarmonyPatch]
    internal static class WeaponRayPatches
    {
        public static BulletWeapon? CachedWeapon = null;
        private static HitData s_hitData = new();

        [HarmonyTargetMethod]
        private static MethodBase FindWeaponRayFunc(Harmony harmony)
        {
            return AccessTools.Method(
                typeof(Weapon),
                nameof(Weapon.CastWeaponRay),
                new Type[] { typeof(Transform), typeof(Weapon.WeaponHitData).MakeByRefType(), typeof(Vector3), typeof(int) }
                );
        }

        private static CustomWeaponComponent? _cachedCWC = null;

        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PreRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            if (CachedWeapon != null)
                _cachedCWC = CachedWeapon.GetComponent<CustomWeaponComponent>();
            else
                _cachedCWC = weaponRayData.owner?.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();

            if (_cachedCWC == null) return;

            s_hitData.Setup(weaponRayData);
            _cachedCWC.Invoke(new WeaponPreRayContext(s_hitData, originPos));
            s_hitData.Apply();
        }

        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask, ref bool __result)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            if (_cachedCWC == null) return;

            s_hitData.Setup(weaponRayData);
            if (!_cachedCWC.Invoke(new WeaponCancelRayContext(s_hitData, originPos)).Result)
            {
                __result = false;
                return;
            }

            __result = _cachedCWC.Invoke(new WeaponPostRayContext(s_hitData, originPos, __result)).Result;
            s_hitData.Apply();
        }
    }
}
