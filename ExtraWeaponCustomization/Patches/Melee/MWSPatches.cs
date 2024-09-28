using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext;
using Gear;
using HarmonyLib;

namespace EWC.Patches.Melee
{
    [HarmonyPatch]
    internal static class MWSPatches
    {
        [HarmonyPatch(typeof(MWS_ChargeUp), nameof(MWS_ChargeUp.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ChargeCallback(MWS_ChargeUp __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            WeaponFireRateContext context = new(1f);
            cwc.Invoke(context);
            __instance.m_maxDamageTime /= context.Value;
        }

        [HarmonyPatch(typeof(MWS_Push), nameof(MWS_Push.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PushCallback(MWS_Push __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            WeaponFireRateContext context = new(1f);
            cwc.Invoke(context);
            __instance.m_weapon.WeaponAnimator.speed *= context.Value;
        }

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

            cwc.Invoke(StaticContext<WeaponPostFireContext>.Instance);
        }
    }
}
