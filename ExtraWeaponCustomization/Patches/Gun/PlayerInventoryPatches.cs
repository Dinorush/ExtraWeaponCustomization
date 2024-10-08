﻿using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;
using Player;

namespace EWC.Patches
{
    [HarmonyPatch]
    internal static class PlayerInventoryPatches
    {
        [HarmonyPatch(typeof(PUI_Inventory), nameof(PUI_Inventory.SetSlotAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void SetAmmoUICallback(PUI_Inventory __instance, InventorySlot slot, ref int clipAbs, ref int inPackAbs, ref float inPackRel)
        {
            if (slot == InventorySlot.None) return;

            CustomWeaponComponent? cwc = __instance.m_owner?
                .PlayerAgent?.TryCast<PlayerAgent>()?
                .FPItemHolder?
                .WieldedItem?.TryCast<BulletWeapon>()?
                .GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            PUI_InventoryItem item = __instance.m_inventorySlots[slot];
            WeaponPreAmmoUIContext context = new(clipAbs, inPackAbs, inPackRel, item.ShowAmmoClip, item.ShowAmmoPack, item.ShowAmmoTotalRel, item.ShowAmmoInfinite);
            cwc.Invoke(context);
            clipAbs = context.Clip;
            inPackAbs = context.Reserve;
            inPackRel = context.TotalRel;
            item.ShowAmmoClip = context.ShowClip;
            item.ShowAmmoPack = context.ShowReserve;
            item.ShowAmmoTotalRel = context.ShowRel;
            item.ShowAmmoInfinite = context.ShowInfinite;
        }
        

        [HarmonyPatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.ReceiveAmmoGive))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void AmmoPackCallback(ref pAmmoGive data)
        {
            if (!data.targetPlayer.TryGetPlayer(out var player)) return;

            PlayerAgent? agent = player.PlayerAgent?.TryCast<PlayerAgent>();
            if (agent == null) return;

            if(PlayerBackpackManager.TryGetItem(player, InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                if (cwc != null)
                {
                    WeaponPreAmmoPackContext context = new(data.ammoStandardRel);
                    cwc.Invoke(context);
                    data.ammoStandardRel = context.AmmoRel;
                }
            }

            if (PlayerBackpackManager.TryGetItem(player, InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                if (cwc != null)
                {
                    WeaponPreAmmoPackContext context = new(data.ammoSpecialRel);
                    cwc.Invoke(context);
                    data.ammoSpecialRel = context.AmmoRel;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.ReceiveAmmoGive))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostAmmoPackCallback(ref pAmmoGive data)
        {
            if (!data.targetPlayer.TryGetPlayer(out var player)) return;

            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(player);
            if (backpack.TryGetBackpackItem(InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoPackContext(backpack.AmmoStorage));
            }

            if (backpack.TryGetBackpackItem(InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoPackContext(backpack.AmmoStorage));
            }
        }

        [HarmonyPatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.DoReload))]
        [HarmonyPatch(typeof(PlayerInventoryBase), nameof(PlayerInventoryBase.DoReload))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ReloadCallback(PlayerInventoryBase __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_wieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponPostReloadContext>.Instance);
        }

        [HarmonyPatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.TriggerReload))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ReloadStartCallback(PlayerInventoryLocal __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_wieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null || !cwc.Weapon.IsReloading) return;

            cwc.Invoke(StaticContext<WeaponReloadStartContext>.Instance);
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.FillAllClips))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostFillAllClipsCallback(PlayerAmmoStorage __instance)
        {
            if (__instance.m_playerBackpack.TryGetBackpackItem(InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoInitContext(__instance, __instance.StandardAmmo));
            }

            if (__instance.m_playerBackpack.TryGetBackpackItem(InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoInitContext(__instance, __instance.SpecialAmmo));
            }
        }
    }
}
