using EWC.CustomWeapon.CustomShot;
using Gear;
using HarmonyLib;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class WeaponCachePatches
    {
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyPriority(Priority.High)]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Fire(BulletWeapon __instance)
        {
            ShotManager.CacheFiringGun(__instance);
        }

        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Fire()
        {
            ShotManager.ClearFiringInfo();
        }
    }
}
