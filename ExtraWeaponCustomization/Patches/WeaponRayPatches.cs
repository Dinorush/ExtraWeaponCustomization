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

        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void RayCallback(Transform alignTransform, ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            CustomWeaponComponent? cwc = weaponRayData.owner?.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPreRayContext(weaponRayData, cwc.Weapon));
        }

        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostRayCallback(Transform alignTransform, ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask, ref bool __result)
        {
            // Sentry filter
            if (altRayCastMask != -1) return;

            CustomWeaponComponent? cwc = weaponRayData.owner?.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            WeaponPostRayContext context = new(weaponRayData, cwc.Weapon);
            cwc.Invoke(context);
            __result &= context.Allow;
        }
    }
}
