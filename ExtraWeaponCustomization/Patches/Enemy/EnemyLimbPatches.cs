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

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Damage(Dam_EnemyDamageLimb __instance)
        {
            ContextController? cc = WeaponPatches.CachedHitCC;
            if (cc == null || __instance.m_type != eLimbDamageType.Armor) return;

            _cachedArmor = __instance.m_armorDamageMulti;
            __instance.m_armorDamageMulti = cc.Invoke(new WeaponArmorContext(_cachedArmor)).ArmorMulti;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Damage(Dam_EnemyDamageLimb __instance)
        {
            if (WeaponPatches.CachedHitCC == null || __instance.m_type != eLimbDamageType.Armor) return;

            __instance.m_armorDamageMulti = _cachedArmor;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb_Custom), nameof(Dam_EnemyDamageLimb_Custom.ApplyWeakspotAndArmorModifiers))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_WeakspotModifiers(Dam_EnemyDamageLimb_Custom __instance, float dam, float precisionMulti, ref float __result)
        {
            if (!WeaponPatches.CachedBypassTumorCap) return true;

            __result = dam * Math.Max(__instance.m_weakspotDamageMulti * precisionMulti, 1) * __instance.m_armorDamageMulti;
            return false;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveGlueDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool FixGlueDamage(Dam_EnemyDamageBase __instance, pMiniDamageData data)
        {
            GlueVolumeDesc glueVolumeDesc = default;
            glueVolumeDesc.volume = data.damage.Get(100f); // Epic .Get(HealthMax) when .Set(100f) is used
            glueVolumeDesc.expandVolume = 0f;
            glueVolumeDesc.currentScale = 0f;
            __instance.AddToTotalGlueVolume(null, glueVolumeDesc);
            return false;
        }
    }
}
