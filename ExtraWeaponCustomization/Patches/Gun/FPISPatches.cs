using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using FirstPersonItem;
using HarmonyLib;

namespace ExtraWeaponCustomization.Patches
{
    [HarmonyPatch]
    internal static class FPISPatches
    {
        // Need to cache weapon since they may exit the Aim state after wielded item changes (e.g. on weapon swap)
        private static CustomWeaponComponent? _cachedCWC;

        [HarmonyPatch(typeof(FPIS_Aim), nameof(FPIS_Aim.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_AimEnter(FPIS_Aim __instance)
        {
            _cachedCWC = __instance.Holder.WieldedItem.GetComponent<CustomWeaponComponent>();
            if (_cachedCWC == null) return;

            _cachedCWC.Invoke(StaticContext<WeaponAimContext>.Instance);
        }

        [HarmonyPatch(typeof(FPIS_Aim), nameof(FPIS_Aim.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_AimExit()
        {
            if (_cachedCWC == null) return;

            _cachedCWC.Invoke(StaticContext<WeaponAimEndContext>.Instance);
        }
    }
}
