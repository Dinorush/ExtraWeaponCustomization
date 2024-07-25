using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.CustomWeapon;
using HarmonyLib;
using static Weapon;
using UnityEngine;
using System.Reflection;
using System;

namespace ExtraWeaponCustomization.Patches
{
    [HarmonyPatch]
    internal static class WeaponRayPatches
    {
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
        private static void PreRayCallback(Transform alignTransform, ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            _cachedCWC = weaponRayData.owner?.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (_cachedCWC == null) return;

            _cachedCWC.Invoke(new WeaponPreRayContext(weaponRayData, _cachedCWC.Weapon));
        }

        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostRayCallback(Transform alignTransform, ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            if (_cachedCWC == null) return;

            _cachedCWC.Invoke(new WeaponPostRayContext(weaponRayData, _cachedCWC.Weapon));
        }
    }
}
