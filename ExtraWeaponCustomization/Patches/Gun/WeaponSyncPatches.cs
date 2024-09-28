using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;

namespace EWC.Patches
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
        [HarmonyPostfix]
        private static void Post_FireSynced(BulletWeaponSynced __instance)
        {
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPreFireContextSync());
            cwc.UpdateStoredFireRate(__instance.m_archeType);
            cwc.ModifyFireRate(__instance);
            cwc.Invoke(new WeaponPostFireContextSync());
        }
    }
}
