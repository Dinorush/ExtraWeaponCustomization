using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;

namespace EWC.Patches.Melee
{
    [HarmonyPatch]
    internal static class MWSPatches
    {
        [HarmonyPatch(typeof(MWS_AttackSwingBase), nameof(MWS_AttackSwingBase.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PreSwingCallback(MWS_AttackSwingBase __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponPreFireContext>.Instance);
        }

        [HarmonyPatch(typeof(MWS_AttackSwingBase), nameof(MWS_AttackSwingBase.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostSwingCallback(MWS_AttackSwingBase __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponShotEndContext(CustomWeapon.Enums.DamageType.Bullet, MeleePatches.HitData.shotInfo, null));
            cwc.Invoke(StaticContext<WeaponPostFireContext>.Instance);
        }
    }
}
