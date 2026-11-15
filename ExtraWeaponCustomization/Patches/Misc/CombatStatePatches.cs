using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext.Contexts;
using HarmonyLib;
using Player;

namespace EWC.Patches.Misc
{
    [HarmonyPatch]
    internal static class CombatStatePatches
    {
        [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ChangeState))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PreChangeDramaState(ref bool __state)
        {
            __state = DramaManager.InActionState;
        }

        [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ChangeState))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostChangeDramaState(bool __state)
        {
            bool newState = DramaManager.InActionState;
            if (__state == newState) return;

            var context = new WeaponCombatContext(newState);
            foreach (var kvp in PlayerBackpackManager.Current.m_backpacks)
                CustomWeaponManager.InvokeOnGear(kvp.value.Owner, context);
        }
    }
}
