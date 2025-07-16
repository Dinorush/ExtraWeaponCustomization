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
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Fire(BulletWeapon __instance)
        {
            if (!__instance.Owner.Owner.IsBot)
                ShotManager.FiringWeapon = __instance;
        }

        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Fire()
        {
            ShotManager.FiringWeapon = null;
        }
    }
}
