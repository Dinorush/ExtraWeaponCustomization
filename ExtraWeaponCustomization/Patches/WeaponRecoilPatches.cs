using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using GameData;
using Gear;
using HarmonyLib;
using UnityEngine;

namespace ExtraWeaponCustomization.Patches
{
    [HarmonyPatch]
    internal static class WeaponRecoilPatches
    {
        private static BulletWeapon? _cachedWeapon;
        private static CustomWeaponComponent? _cachedComponent;

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.OnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateCurrentWeapon(BulletWeapon __instance)
        {
            if (__instance.Owner?.IsLocallyOwned != true) return;

            _cachedWeapon = __instance;
            _cachedComponent = __instance.GetComponent<CustomWeaponComponent>();
        }

        [HarmonyAfter("Dinorush.ExtraRecoilData")]
        [HarmonyPatch(typeof(FPS_RecoilSystem), nameof(FPS_RecoilSystem.ApplyRecoil))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostApplyRecoilCallback(FPS_RecoilSystem __instance, bool resetSimilarity, RecoilDataBlock recoilData)
        {
            // Basically the same function ExtraRecoilData runs, but we have a different component dependency.
            // Needs to run after ExtraRecoilData since that overwrites existing recoil.

            if (_cachedComponent == null) return;

            WeaponRecoilContext context = new(_cachedWeapon!);
            _cachedComponent.Invoke(context);
            if (context.Value == 1f) return;

            Vector2 deltaDir = new(__instance.recoilDir.x * (context.Value - 1f), __instance.recoilDir.y * (context.Value - 1f));

            Vector2 newForce = __instance.currentRecoilForce;
            newForce.x -= deltaDir.x * (1f - recoilData.worldToViewSpaceBlendVertical);
            newForce.y -= deltaDir.y * (1f - recoilData.worldToViewSpaceBlendHorizontal);

            Vector2 newForceVP = __instance.currentRecoilForceVP;
            newForceVP.x -= deltaDir.x * recoilData.worldToViewSpaceBlendVertical;
            newForceVP.y -= deltaDir.y * recoilData.worldToViewSpaceBlendHorizontal;

            __instance.recoilDir.Set(__instance.recoilDir.x * context.Value, __instance.recoilDir.y * context.Value);
            __instance.currentRecoilForce = newForce;
            __instance.currentRecoilForceVP = newForceVP;
        }
    }
}
