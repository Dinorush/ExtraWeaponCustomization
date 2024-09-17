using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using HarmonyLib;

namespace ExtraWeaponCustomization.Patches
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
            WeaponArmorContext context = new(_cachedArmor);
            cwc.Invoke(context);
            __instance.m_armorDamageMulti = context.ArmorMulti;
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
    }
}
