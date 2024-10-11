using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext.Contexts;
using HarmonyLib;
using UnityEngine;

namespace EWC.Patches
{
    [HarmonyPatch]
    internal static class EnemyLimbPatches
    {
        private static float _cachedArmor = 0f;

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Damage(Dam_EnemyDamageLimb __instance)
        {
            CustomWeaponComponent? cwc = WeaponPatches.CachedHitCWC;
            if (cwc == null || __instance.m_type != eLimbDamageType.Armor) return;

            _cachedArmor = __instance.m_armorDamageMulti;
            __instance.m_armorDamageMulti = cwc.Invoke(new WeaponArmorContext(_cachedArmor)).ArmorMulti;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Damage(Dam_EnemyDamageLimb __instance)
        {
            if (WeaponPatches.CachedHitCWC == null || __instance.m_type != eLimbDamageType.Armor) return;

            __instance.m_armorDamageMulti = _cachedArmor;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb_Custom), nameof(Dam_EnemyDamageLimb_Custom.ApplyWeakspotAndArmorModifiers))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_WeakspotModifiers(Dam_EnemyDamageLimb_Custom __instance, float dam, float precisionMulti, ref float __result)
        {
            if (!WeaponPatches.CachedBypassTumorCap) return true;

            __result = dam * Mathf.Max(__instance.m_weakspotDamageMulti * precisionMulti, 1) * __instance.m_armorDamageMulti;
            return false;
        }
    }
}
