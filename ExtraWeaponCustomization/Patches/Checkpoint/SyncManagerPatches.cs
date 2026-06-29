using HarmonyLib;
using System;

namespace EWC.Patches.Checkpoint
{
    [HarmonyPatch]
    internal static class SyncManagerPatches
    {
        public static event Action? OnCheckpointReached;
        public static event Action? OnCheckpointReloaded;

        [HarmonyPatch(typeof(CheckpointManager), nameof(CheckpointManager.OnStateChange))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        static void Pre_StateChanged(pCheckpointState oldState, pCheckpointState newState, bool isRecall)
        {
            if (!oldState.isReloadingCheckpoint && !newState.isReloadingCheckpoint)
            {
                // Ignore cases:
                // Client syncs on drop with isRecall: true.
                // Client runs a redundant StoreCheckpoint call w/ no changes prior to any change.
                if (isRecall || oldState.doorLockPosition == newState.doorLockPosition)
                    return;

                OnCheckpointReached?.Invoke();
            }
            else if (oldState.isReloadingCheckpoint && isRecall)
            {
                OnCheckpointReloaded?.Invoke();
            }
        }
    }
}
