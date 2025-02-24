using EWC.CustomWeapon;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using FX_EffectSystem;
using Gear;
using HarmonyLib;
using Player;
using static GameData.GD;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class WeaponArchetypePatches
    {
        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.SetOwner))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(BulletWeaponArchetype __instance, PlayerAgent owner)
        {
            if (owner == null) return;

            CustomWeaponComponent cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.OwnerInit();
        }

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStartFiring))]
        [HarmonyPatch(typeof(BWA_Auto), nameof(BWA_Auto.OnStartFiring))]
        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.OnStartFiring))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool StartFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return true;

            cwc.CancelShot = false;
            WeaponPreStartFireContext context = new();
            cwc.Invoke(context);
            cwc.UpdateStoredFireRate(); // Need to update prior to firing to predict weapon sound delay
            if (!context.Allow)
                cwc.StoreCancelShot();

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

            if (cwc == null) return;
            if (cwc.ResetShotIfCancel(__instance))
            {
                cwc.CancelShot = false;
                __instance.m_readyToFire = false; // Prevent anything else from running (no need to let it run if cancelled)
                return;
            }

            cwc.Invoke(StaticContext<WeaponPostStartFireContext>.Instance);
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

            WeaponFireCancelContext context = new();
            cwc.Invoke(context);
            if (!context.Allow)
                cwc.StoreCancelShot();
            else
                cwc.Invoke(StaticContext<WeaponPreFireContext>.Instance);

            return context.Allow;
        }

        [HarmonyPatch(typeof(BWA_Auto), nameof(BWA_Auto.OnFireShot))]
        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnFireShot))]
        [HarmonyPatch(typeof(BWA_Semi), nameof(BWA_Semi.OnFireShot))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null || cwc.CancelShot) return;

            cwc.NotifyShotFired();
            if (!cwc.Invoke(new WeaponCancelTracerContext()).Allow)
                ShotManager.CancelTracerFX(__instance.m_archetypeData, __instance.m_weapon!.TryCast<Shotgun>() != null);

            cwc.Invoke(StaticContext<WeaponPostFireContext>.Instance);
        }

        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.PostFireCheck))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PrePostFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.ResetShotIfCancel(__instance); // Need reset stuff here so post fire check correctly stops firing
        }

        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.PostFireCheck))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostPostFireCallback(BulletWeaponArchetype __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;
            if (cwc.CancelShot)
            {
                cwc.ModifyFireRate();
                cwc.CancelShot = false;
                return;
            }

            cwc.UpdateStoredFireRate();
            cwc.ModifyFireRate();
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

            cwc.Invoke(StaticContext<WeaponPostStopFiringContext>.Instance);
        }
    }
}
