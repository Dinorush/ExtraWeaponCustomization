using EWC.CustomWeapon.CustomShot;
using HarmonyLib;
using Player;

namespace EWC.Patches.Player
{
    [HarmonyPatch]
    internal static class PlayerSpawnPatches
    {
        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SpawnPlayerCallback(PlayerSync __instance)
        {
            AccuracyManager.UpdatePlayerAgent(__instance.m_agent);
        }
    }
}
