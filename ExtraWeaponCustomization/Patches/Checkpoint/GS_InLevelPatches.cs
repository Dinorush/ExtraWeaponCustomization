using EWC.CustomWeapon;
using HarmonyLib;

namespace EWC.Patches.Checkpoint
{
    [HarmonyPatch]
    internal static class GS_InLevelPatches
    {
        // For some reason weapon data (e.g. Archetype) are not stored when the CapureGameState runs.
        // Clearing everything when leaving the level is technically overkill, but EWC doesn't care at that point.
        [HarmonyPatch(typeof(GS_InLevel), nameof(GS_InLevel.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void ClearModifications()
        {
            CustomWeaponManager.ResetCWCs(false);
        }
    }
}
