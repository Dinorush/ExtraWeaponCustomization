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
        private static float _cachedArmor = -1f;
        // Cached variables are set for each call that would require them (i.e. once per BulletHit) and cleared after use
        private static CustomWeaponComponent? _cachedCWC = null;
        private static ContextController? _cachedCC = null;
        public static void CacheComponents(CustomWeaponComponent cwc, ContextController cc)
        {
            _cachedCWC = cwc;
            _cachedCC = cc;
        }

        public static void ShowHitmarker(ContextController cc, Dam_EnemyDamageLimb limb, bool crit, bool willKill, UnityEngine.Vector3 hitPos, bool armor)
        {
            var oldCC = _cachedCC;
            _cachedCC = cc;
            limb.ShowHitIndicator(crit, willKill, hitPos, armor);
            _cachedCC = oldCC;
        }

        public static bool CachedBypassTumorCap { get; set; } = false;

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Damage(Dam_EnemyDamageLimb __instance)
        {
            if (__instance.m_armorDamageMulti >= 1f) return;

            _cachedArmor = __instance.m_armorDamageMulti;
            float armorMulti;
            if (_cachedCC != null)
            {
                armorMulti = _cachedCC.Invoke(new WeaponArmorContext(_cachedArmor)).ArmorMulti;
                DebuffManager.GetAndApplyArmorShredDebuff(ref armorMulti, __instance.Cast<IDamageable>(), _cachedCWC!.DebuffIDs);
            }
            else
            {
                armorMulti = _cachedArmor;
                DebuffManager.GetAndApplyArmorShredDebuff(ref armorMulti, __instance.Cast<IDamageable>(), DebuffManager.DefaultGroupSet);
            }

            if (_cachedArmor == armorMulti)
                _cachedArmor = -1;
            else
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
            _cachedCC = null;
            _cachedCWC = null;
            
            if (_cachedArmor == -1) return;
            __instance.m_armorDamageMulti = _cachedArmor;
            _cachedArmor = -1;
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
