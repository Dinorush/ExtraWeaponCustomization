using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using HarmonyLib;

namespace EWC.Patches.Player
{
    [HarmonyPatch]
    internal static class PlayerDamagePatches
    {
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_TakeDamage(Dam_PlayerDamageBase __instance, float damage, ref bool __state)
        {
            var owner = __instance.Owner.Owner;
            __state = damage > 0 && __instance.Health > 0 && (owner.IsLocal || (owner.IsBot && SNetwork.SNet.IsMaster));
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_TakeDamage(Dam_PlayerDamageBase __instance, float damage, bool __state)
        {
            if (!__state) return;

            CustomWeaponManager.InvokeOnGear(__instance.Owner.Owner, new WeaponDamageTakenContext(damage));
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
        [HarmonyPrefix]
        private static void Pre_ReceiveHealth(Dam_PlayerDamageBase __instance, ref float __state)
        {
            __state = __instance.Health;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_ReceiveHealth(Dam_PlayerDamageBase __instance, float __state)
        {
            if (__state == __instance.Health) return;

            CustomWeaponManager.InvokeOnGear(__instance.Owner.Owner, new WeaponHealthContext(__instance));
        }
    }
}
