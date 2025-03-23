using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using HarmonyLib;

namespace EWC.Patches.Player
{
    internal static class PlayerDamagePatches
    {
        private static bool _ignoreCall = false;
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_TakeDamage(Dam_PlayerDamageBase __instance, float damage)
        {
            _ignoreCall = damage <= 0 || __instance.Health <= 0 || !__instance.Owner.IsLocallyOwned;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_TakeDamage(Dam_PlayerDamageBase __instance, float damage)
        {
            if (_ignoreCall) return;

            CustomWeaponManager.InvokeOnGear(__instance.Owner.Owner, new WeaponDamageTakenContext(damage));
        }
    }
}
