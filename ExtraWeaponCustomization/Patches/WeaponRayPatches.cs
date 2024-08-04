using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.CustomWeapon;
using HarmonyLib;
using static Weapon;
using UnityEngine;
using System.Reflection;
using System;
using Gear;
using ExtraWeaponCustomization.Utils;

namespace ExtraWeaponCustomization.Patches
{
    [HarmonyPatch]
    internal static class WeaponRayPatches
    {
        public static BulletWeapon? CachedWeapon = null;

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

            _cachedCWC.Invoke(new WeaponPreRayContext(weaponRayData, originPos, _cachedCWC.Weapon));
        }

        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask, ref bool __result)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            if (_cachedCWC == null) return;

            WeaponPostRayContext context = new(weaponRayData, originPos, _cachedCWC.Weapon, __result);
            _cachedCWC.Invoke(context);
            __result = context.Result;
        }
    }
}
