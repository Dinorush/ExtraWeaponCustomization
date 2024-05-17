using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Gear;
using HarmonyLib;

namespace ExtraWeaponCustomization.Patches
{
    internal static class WeaponArchetypePatches
    {

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStartFiring))]
        [HarmonyPatch(typeof(BWA_Auto), nameof(BWA_Auto.OnStartFiring))]
        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.OnStartFiring))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool StartFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return true;

            WeaponPreFireContext context = new(__instance.m_weapon!);
            cwc.Invoke(context);
            if (!context.Allow)
            {
                cwc.StoreCancelShot(__instance);
                __instance.m_readyToFire = false;
            }

            return context.Allow;
        }

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStartFiring))]
        [HarmonyPatch(typeof(BWA_Auto), nameof(BWA_Auto.OnStartFiring))]
        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.OnStartFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostStartFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null || cwc.CancelShot) return;

            cwc.Invoke(new WeaponPostStartFireContext(__instance.m_weapon!));
        }

        [HarmonyPatch(typeof(BWA_Auto), nameof(BWA_Auto.OnFireShot))]
        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnFireShot))]
        [HarmonyPatch(typeof(BWA_Semi), nameof(BWA_Semi.OnFireShot))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool PreFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return true;
            if (cwc.CancelShot) return false;

            WeaponPreFireContext context = new(__instance.m_weapon!);
            cwc.Invoke(context);
            if (!context.Allow)
                cwc.StoreCancelShot(__instance);
            cwc.UpdateStoredFireRate();

            return context.Allow;
        }

        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.PostFireCheck))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PostFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null || cwc.FlushCancelShot(__instance)) return;

            cwc.ModifyFireRate(__instance);
            cwc.Invoke(new WeaponPostFireContext(__instance.m_weapon!));
        }

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStopFiring))]
        [HarmonyPatch(typeof(BWA_Auto), nameof(BWA_Auto.OnStopFiring))]
        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.OnStopFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void StopFiringCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPostStopFiringContext(__instance.m_weapon!));
        }
    }
}
