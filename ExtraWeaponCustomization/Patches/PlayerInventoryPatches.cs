using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;
using Player;

namespace ExtraWeaponCustomization.Patches
{
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
            WeaponPreAmmoUIContext context = new(cwc.Weapon, clipAbs, inPackAbs, inPackRel, item.ShowAmmoClip, item.ShowAmmoPack, item.ShowAmmoTotalRel, item.ShowAmmoInfinite);
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
        private static void AmmoPackCallback(PlayerBackpackManager __instance, ref pAmmoGive data)
        {
            if (!data.targetPlayer.TryGetPlayer(out var player)) return;

            PlayerAgent? agent = player.PlayerAgent?.TryCast<PlayerAgent>();
            if (agent == null) return;

            if(PlayerBackpackManager.TryGetItem(player, InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                if (cwc != null)
                {
                    WeaponPreAmmoPackContext context = new(cwc.Weapon, data.ammoStandardRel);
                    cwc.Invoke(context);
                    data.ammoStandardRel = context.AmmoRel;
                }
            }

            if (PlayerBackpackManager.TryGetItem(player, InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                if (cwc != null)
                {
                    WeaponPreAmmoPackContext context = new(cwc.Weapon, data.ammoSpecialRel);
                    cwc.Invoke(context);
                    data.ammoSpecialRel = context.AmmoRel;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.ReceiveAmmoGive))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostAmmoPackCallback(PlayerBackpackManager __instance, ref pAmmoGive data)
        {
            if (!data.targetPlayer.TryGetPlayer(out var player)) return;

            PlayerAgent? agent = player.PlayerAgent?.TryCast<PlayerAgent>();
            if (agent == null) return;

            if (PlayerBackpackManager.TryGetItem(player, InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoPackContext(cwc.Weapon));
            }

            if (PlayerBackpackManager.TryGetItem(player, InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoPackContext(cwc.Weapon));
            }
        }

        [HarmonyPatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.DoReload))]
        [HarmonyPatch(typeof(PlayerInventoryBase), nameof(PlayerInventoryBase.DoReload))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ReloadCallback(PlayerInventoryBase __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_wieldedItem?.TryCast<BulletWeapon>()?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPostReloadContext(cwc.Weapon));
        }
    }
}
