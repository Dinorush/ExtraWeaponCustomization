using HarmonyLib;
using SNetwork;
using System;

namespace EWC.Patches.SNet
{
    [HarmonyPatch]
    internal static class SyncManagerPatches
    {
        public static event Action? OnCheckpointReload;

        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnRecallDone))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Pre_Reload()
        {
            OnCheckpointReload?.Invoke();
        }
    }
}
