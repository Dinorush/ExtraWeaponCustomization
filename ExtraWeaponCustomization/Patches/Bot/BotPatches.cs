using EWC.Patches.Gun;
using HarmonyLib;
using Player;

namespace EWC.Patches.Bot
{
    [HarmonyPatch]
    internal static class BotPatches
    {
        [HarmonyPatch(typeof(PlayerBotActionUseFirearm), nameof(PlayerBotActionUseFirearm.StartShooting))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void OnStartFiring(PlayerBotActionUseFirearm __instance)
        {
            WeaponSyncPatches.FlagStartFiring(__instance.m_bot);
        }

        [HarmonyPatch(typeof(PlayerBotActionUseFirearm), nameof(PlayerBotActionUseFirearm.FireOneShot))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnShotQueued(PlayerBotActionUseFirearm __instance)
        {
            if (__instance.m_autoFireBulletsLeft == 1)
                WeaponSyncPatches.FlagEndFiring(__instance.m_bot);
        }
    }
}
