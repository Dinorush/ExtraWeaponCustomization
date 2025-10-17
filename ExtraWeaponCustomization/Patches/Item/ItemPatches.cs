using EWC.Attributes;
using EWC.CustomWeapon;
using EWC.Utils.Extensions;
using Gear;
using HarmonyLib;

namespace EWC.Patches.Item
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.OnDestroy))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnGearDestroyed(ItemEquippable __instance)
        {
            CustomWeaponManager.RemoveSpawnedItem(__instance);
        }

        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.OnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateCurrentWeapon(ItemEquippable __instance)
        {
            if (!__instance.TryGetComp<CustomWeaponComponent>(out var cwc)) return;

            cwc.OnWield();
        }

        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.OnUnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateWeaponUnwielded(BulletWeapon __instance)
        {
            if (!__instance.TryGetComp<CustomWeaponComponent>(out var cwc)) return;

            cwc.OnUnWield();
        }
    }
}
