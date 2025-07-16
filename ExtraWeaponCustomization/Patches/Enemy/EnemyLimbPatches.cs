using EWC.CustomWeapon;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using HarmonyLib;
using System;

namespace EWC.Patches.Enemy
{
    [HarmonyPatch]
    internal static class EnemyLimbPatches
    {
        private static float _cachedArmor = 0f;
        // Cached variables are set for each call that would require them (i.e. once per BulletHit) and cleared after use
        private static CustomWeaponComponent? _cachedCWC = null;
        private static ContextController? _cachedCC = null;
        public static void CacheComponents(CustomWeaponComponent cwc, ContextController cc)
        {
            _cachedCWC = cwc;
            _cachedCC = cc;
        }

        public static bool CachedBypassTumorCap { get; set; } = false;

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Damage(Dam_EnemyDamageLimb __instance)
        {
            if (_cachedCC == null || __instance.m_type != eLimbDamageType.Armor) return;

            _cachedArmor = __instance.m_armorDamageMulti;
            
            var armorMulti = _cachedCC.Invoke(new WeaponArmorContext(_cachedArmor)).ArmorMulti;
            if (armorMulti < 1f && DebuffManager.TryGetArmorShredDebuff(__instance.Cast<IDamageable>(), _cachedCWC!.DebuffIDs, out var armorEffect))
                armorMulti = 1f - (1f - armorMulti) * Math.Clamp(armorEffect, 0, 1);
            __instance.m_armorDamageMulti = armorMulti;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.ShowHitIndicator))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_ShowHitMarker(Dam_EnemyDamageLimb __instance, bool willDie)
        {
            if (willDie) return true;
            return _cachedCC == null || _cachedCC.Invoke(new WeaponHitmarkerContext(__instance.m_base.Owner)).Result;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Damage(Dam_EnemyDamageLimb __instance)
        {
            if (_cachedCC == null) return;
            _cachedCC = null;
            _cachedCWC = null;
            
            if (__instance.m_type != eLimbDamageType.Armor) return;
            __instance.m_armorDamageMulti = _cachedArmor;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb_Custom), nameof(Dam_EnemyDamageLimb_Custom.ApplyWeakspotAndArmorModifiers))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_WeakspotModifiers(Dam_EnemyDamageLimb_Custom __instance, float dam, float precisionMulti, ref float __result)
        {
            if (!CachedBypassTumorCap) return true;

            __result = dam * Math.Max(__instance.m_weakspotDamageMulti * precisionMulti, 1) * __instance.m_armorDamageMulti;
            CachedBypassTumorCap = false;
            return false;
        }
    }
}
