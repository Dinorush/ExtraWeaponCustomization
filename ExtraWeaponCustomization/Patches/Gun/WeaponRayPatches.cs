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

        private static CustomWeaponComponent? _cachedCWC = null;
        private static IntPtr _cachedData = IntPtr.Zero;

        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PreRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, int altRayCastMask)
        {
            // Sentry filter
            if (altRayCastMask != -1 || _cachedData == weaponRayData.Pointer) return;
            _cachedData = weaponRayData.Pointer;

            if (ShotManager.CachedShotgun != null)
                _cachedCWC = ShotManager.CachedShotgun.GetComponent<CustomWeaponComponent>();
            else
                _cachedCWC = weaponRayData.owner?.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();

            if (_cachedCWC == null) return;

            s_hitData.Setup(weaponRayData, _cachedCWC);
            _cachedCWC.Invoke(new WeaponPreRayContext(s_hitData, originPos));
            float mod = _cachedCWC.SpreadController!.Value;
            if (mod != 1f)
            {
                s_hitData.randomSpread *= mod;
                s_hitData.angOffsetX *= mod;
                s_hitData.angOffsetY *= mod;
            }

            s_hitData.Apply();
        }

        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostRayCallback(ref WeaponHitData weaponRayData, Vector3 originPos, ref bool __result)
        {
            if (_cachedCWC == null) return;

            s_hitData.Setup(weaponRayData, _cachedCWC);
            if (_cachedCWC.ShotComponent!.OverrideVanillaShot)
            {
                _cachedCWC.ShotComponent.FireVanilla(s_hitData, originPos);
                __result = false;
                return;
            }

            __result = _cachedCWC.Invoke(new WeaponPostRayContext(s_hitData, originPos, __result)).Result;
            s_hitData.Apply();
            _cachedCWC = null;
        }
    }
}
