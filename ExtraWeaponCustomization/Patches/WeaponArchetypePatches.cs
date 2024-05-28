using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;
using Player;

namespace ExtraWeaponCustomization.Patches
{
    internal static class WeaponArchetypePatches
    {
        [HarmonyPatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.SetOwner))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(BulletWeaponArchetype __instance, PlayerAgent owner)
        {
            if (owner == null) return;

            CustomWeaponData? data = CustomWeaponManager.Current.GetCustomWeaponData(__instance.m_archetypeData.persistentID);
            if (data == null) return;

            if (__instance.m_weapon.gameObject.GetComponent<CustomWeaponComponent>() != null) return;

            CustomWeaponComponent cwc = __instance.m_weapon.gameObject.AddComponent<CustomWeaponComponent>();
            cwc.Register(data);
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

            WeaponPreStartFireContext context = new(__instance.m_weapon!);
            cwc.Invoke(context);
            cwc.UpdateStoredFireRate(__instance); // Need to update prior to firing to predict weapon sound delay
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
                cwc.StoreCancelShot();

            return context.Allow;
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
                cwc.CancelShot = false;
                return;
            }

            cwc.UpdateStoredFireRate(__instance);
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
