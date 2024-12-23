using Gear;
using HarmonyLib;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class ShotgunPatches
    {
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Fire(Shotgun __instance)
        {
            WeaponRayPatches.CachedWeapon = __instance;
        }

        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Fire(Shotgun __instance)
        {
            WeaponRayPatches.CachedWeapon = null;
        }
    }
}
