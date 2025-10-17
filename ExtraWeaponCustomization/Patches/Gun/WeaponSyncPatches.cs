using EWC.Attributes;
using EWC.CustomWeapon;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class WeaponSyncPatches
    {
        public static readonly Dictionary<IntPtr, (bool start, bool end)> _botFireFlags = new(3);

        [InvokeOnCleanup]
        private static void Cleanup()
        {
            _botFireFlags.Clear();
        }

        public static void FlagStartFiring(PlayerAIBot bot)
        {
            var ptr = bot.Agent.Pointer;
            if (!_botFireFlags.TryGetValue(ptr, out var flags))
                flags = (false, false);
            _botFireFlags[bot.Agent.Pointer] = (flags.start, true);
        }

        public static void FlagEndFiring(PlayerAIBot bot)
        {
            var ptr = bot.Agent.Pointer;
            if (!_botFireFlags.TryGetValue(ptr, out var flags))
                flags = (false, false);
            _botFireFlags[bot.Agent.Pointer] = (true, flags.end);
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_FireSynced(BulletWeaponSynced __instance)
        {
            var owner = __instance.Owner;
            bool managed = owner.Owner.IsBot && SNetwork.SNet.IsMaster;
            CustomGunComponent? cgc = __instance.GetComponent<CustomGunComponent>();
            if (cgc == null) return;

            if (managed)
            {
                bool isStart = _botFireFlags.TryGetValue(owner.Pointer, out var flags) && flags.start;
                if (isStart)
                {
                    cgc.Invoke(StaticContext<WeaponPreStartFireContext>.Instance);
                    _botFireFlags[owner.Pointer] = (false, flags.end);
                }
                cgc.UpdateStoredFireRate();
                if (isStart)
                    cgc.Invoke(StaticContext<WeaponPostStartFireContext>.Instance);

                cgc.Invoke(StaticContext<WeaponPreFireContext>.Instance);
                ShotManager.AdvanceGroupMod(cgc);
            }
            else
            {
                cgc.Invoke(StaticContext<WeaponPreFireContextSync>.Instance);
                cgc.UpdateStoredFireRate();
            }
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(ShotgunSynced.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_FireSynced(BulletWeaponSynced __instance)
        {
            var owner = __instance.Owner;
            bool managed = owner.Owner.IsBot && SNetwork.SNet.IsMaster;
            CustomGunComponent? cgc = __instance.GetComponent<CustomGunComponent>();
            if (cgc == null) return;

            cgc.NotifyShotFired();
            ShotManager.CancelTracerFX(cgc);
            if (managed)
            {
                ShotManager.RunVanillaShotEnd();
                cgc.Invoke(new WeaponAmmoContext(__instance.m_clip, __instance.ClipSize));
                cgc.Invoke(StaticContext<WeaponPostFireContext>.Instance);
                if (_botFireFlags.TryGetValue(__instance.Owner.Pointer, out var flags) && flags.end)
                {
                    cgc.Invoke(StaticContext<WeaponPostStopFiringContext>.Instance);
                    _botFireFlags[owner.Pointer] = (flags.start, false);
                }
            }

            if (!managed)
                cgc.Invoke(StaticContext<WeaponPostFireContextSync>.Instance);
            cgc.ModifyFireRate();
        }
    }
}
