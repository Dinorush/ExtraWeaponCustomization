using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class WeaponSyncPatches
    {
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SetupSynced(BulletWeaponSynced __instance)
        {
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.SetToSync();
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_FireSynced(BulletWeaponSynced __instance)
        {
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponPreFireContextSync>.Instance);
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_FireSynced(BulletWeaponSynced __instance)
        {
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.NotifyShotFired();
            cwc.UpdateStoredFireRate();
            cwc.ModifyFireRateSynced(__instance);
            cwc.Invoke(StaticContext<WeaponPostFireContextSync>.Instance);
        }
    }
}
