using HarmonyLib;

namespace ExtraWeaponCustomization.Patches
{
    [HarmonyPatch]
    internal static class PlayerLocalPatches
    {
        // Bug fix health getting stuck at red
        [HarmonyPatch(typeof(PUI_LocalPlayerStatus), nameof(PUI_LocalPlayerStatus.UpdateHealth))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void UpdateHealth(PUI_LocalPlayerStatus __instance, float health, bool meleeBuffActive)
        {
            if (__instance.m_lastHealthVal <= 0.14f && health > 0.14f && __instance.m_warningRoutine != null)
                __instance.m_healthWarningLooping = true;
        }
    }
}
