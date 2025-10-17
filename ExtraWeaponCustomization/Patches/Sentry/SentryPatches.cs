using EWC.CustomWeapon;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using HarmonyLib;

namespace EWC.Patches.Sentry
{
    [HarmonyPatch]
    internal static class SentryPatches
    {
        [HarmonyPatch(typeof(SentryGunFirstPerson), nameof(SentryGunFirstPerson.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnGearSpawn(SentryGunFirstPerson __instance)
        {
            CustomWeaponManager.AddSpawnedItem(__instance);
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostSpawnCallback(SentryGunInstance __instance)
        {
            CustomWeaponManager.ActivateSentry(__instance);
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.SyncedPickup))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PrePickupCallback(SentryGunInstance __instance)
        {
            CustomWeaponManager.RemoveSentry(__instance);
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.StartFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostStartFiring(SentryGunInstance __instance)
        {
            if (!__instance.TryGetComp<CustomGunComponent>(out var cgc)) return;

            ((SentryGunComp) cgc.Gun).IsFirstShot = true;
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.StopFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostStopFiring(SentryGunInstance __instance)
        {
            if (!__instance.TryGetComp<CustomGunComponent>(out var cgc)) return;

            var gun = ((SentryGunComp)cgc.Gun);
            if (!gun.IsFirstShot)
                cgc.Invoke(StaticContext<WeaponPostStopFiringContext>.Instance);
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireMaster))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PreFireCallback(SentryGunInstance_Firing_Bullets __instance, bool targetIsTagged, out (CustomGunComponent? cgc, bool fired) __state)
        {
            var time = Clock.Time;
            if (!__instance.TryGetComp<CustomGunComponent>(out var cgc))
            {
                if (__instance.m_fireBulletTimer < time && __instance.m_burstTimer < time)
                {
                    ShotManager.CacheFiringSentry(__instance.m_core.Cast<SentryGunInstance>(), isTagged: targetIsTagged);
                    __state = (null, true);
                }
                else
                    __state = (null, false);
                return;
            }

            var gun = (SentryGunComp)cgc.Gun;
            bool burst = gun.FireMode == eWeaponFireMode.Burst;
            bool inBurst = !burst || __instance.m_burstTimer < time;
            bool burstIsDone = burst && __instance.m_burstClipCurr == 0;
            if (!burstIsDone && inBurst && __instance.m_fireBulletTimer < time)
            {
                ShotManager.CacheFiringSentry(gun.Value, isTagged: targetIsTagged);
                if (gun.IsFirstShot)
                    cgc.Invoke(StaticContext<WeaponPreStartFireContext>.Instance);
                cgc.UpdateStoredFireRate(targetIsTagged);
                if (gun.IsFirstShot)
                    cgc.Invoke(StaticContext<WeaponPostStartFireContext>.Instance);

                cgc.Invoke(StaticContext<WeaponPreFireContext>.Instance);
                ShotManager.AdvanceGroupMod(cgc, targetIsTagged);
                __state = (cgc, true);
            }
            else if (burstIsDone && inBurst)
                __state = (cgc, false);
            else
                __state = (null, false);
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireMaster))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostFireCallback(SentryGunInstance_Firing_Bullets __instance, (CustomGunComponent? cgc, bool fired) __state)
        {
            var cgc = __state.cgc;
            if (cgc == null)
            {
                if (__state.fired)
                    ShotManager.ClearFiringInfo();
                return;
            }

            if (__state.fired)
            {
                cgc.NotifyShotFired();
                ShotManager.CancelTracerFX(cgc);

                var core = __instance.m_core;
                float cost = core.CostOfBullet;
                cgc.Invoke(new WeaponAmmoContext((int) (core.Ammo / cost), (int) (core.AmmoMaxCap / cost)));
                cgc.Invoke(StaticContext<WeaponPostFireContext>.Instance);
                cgc.ModifyFireRate();
                ShotManager.ClearFiringInfo();
            }
            else // Burst has ended
            {
                cgc.Invoke(StaticContext<WeaponPostStopFiringContext>.Instance);
                ((SentryGunComp)cgc.Gun).IsFirstShot = true;
                cgc.ModifyFireRate();
            }
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireClient))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PreFireClientCallback(SentryGunInstance_Firing_Bullets __instance, out (CustomGunComponent? cgc, bool fired) __state)
        {
            var time = Clock.Time;
            if (!__instance.TryGetComp<CustomGunComponent>(out var cgc))
            {
                if (__instance.m_fireBulletTimer < time && __instance.m_burstTimer < time)
                {
                    ShotManager.CacheFiringSentry(__instance.m_core.Cast<SentryGunInstance>(), isTagged: false);
                    __state = (null, true);
                }
                else
                    __state = (null, false);
                return;
            }

            bool burst = cgc.Gun.FireMode == eWeaponFireMode.Burst;
            bool inBurst = !burst || __instance.m_burstTimer < time;
            bool burstIsDone = burst && __instance.m_burstClipCurr == 0;
            if (!burstIsDone && inBurst && __instance.m_fireBulletTimer < time)
            {
                ShotManager.CacheFiringSentry(((SentryGunComp)cgc.Gun).Value, isTagged: false);
                cgc.Invoke(StaticContext<WeaponPreFireContextSync>.Instance);
                cgc.UpdateStoredFireRate();
                __state = (cgc, true);
            }
            else if (burstIsDone && inBurst)
                __state = (cgc, false);
            else
                __state = (null, false);
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireClient))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostFireClientCallback(SentryGunInstance_Firing_Bullets __instance, (CustomGunComponent? cgc, bool fired) __state)
        {
            var cgc = __state.cgc;
            if (cgc == null)
            {
                ShotManager.ClearFiringInfo();
                return;
            }

            if (__state.fired)
            {
                cgc.NotifyShotFired();
                ShotManager.CancelTracerFX(cgc);
                cgc.Invoke(StaticContext<WeaponPostFireContextSync>.Instance);
                cgc.ModifyFireRate();
                ShotManager.ClearFiringInfo();
            }
            else // Burst has ended
            {
                cgc.ModifyFireRate();
            }
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.GiveAmmoRel))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void UpdateClip(SentryGunInstance __instance, ref float ammoClassRel, out CustomGunComponent? __state)
        {
            bool gotAmmo = (int) (__instance.Ammo / __instance.CostOfBullet) != (int)(__instance.AmmoMaxCap / __instance.CostOfBullet);

            if (gotAmmo && __instance.TryGetComp<CustomGunComponent>(out __state))
                ammoClassRel = __state.Invoke(new WeaponPreAmmoPackContext(ammoClassRel)).AmmoAmount;
            else
                __state = null;
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.GiveAmmoRel))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateAmmo(SentryGunInstance __instance, CustomGunComponent? __state)
        {
            if (__state == null) return;

            float cost = __instance.CostOfBullet;
            __state.Invoke(new WeaponAmmoContext((int)(__instance.Ammo / cost), (int)(__instance.AmmoMaxCap / cost)));
        }
    }
}
