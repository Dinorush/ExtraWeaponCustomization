using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using HarmonyLib;
using Player;

namespace EWC.Patches.Player
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

        private static InventorySlot AmmoToSlot(AmmoType ammo)
        {
            return ammo switch
            {
                AmmoType.Standard => InventorySlot.GearStandard,
                AmmoType.Special => InventorySlot.GearSpecial,
                AmmoType.Class => InventorySlot.GearClass,
                _ => InventorySlot.None
            };
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.PickupAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void AmmoPackCallback(PlayerAmmoStorage __instance, AmmoType ammoType, ref float ammoAmount)
        {
            if (__instance.m_playerBackpack.TryGetBackpackItem(AmmoToSlot(ammoType), out BackpackItem item))
            {
                CustomWeaponComponent? cwc = item.Instance?.GetComponent<CustomWeaponComponent>();
                if (cwc != null)
                    ammoAmount = cwc.Invoke(new WeaponPreAmmoPackContext(ammoAmount)).AmmoAmount;
            }
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.PickupAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostAmmoPackCallback(PlayerAmmoStorage __instance, AmmoType ammoType)
        {
            if (__instance.m_playerBackpack.TryGetBackpackItem(AmmoToSlot(ammoType), out BackpackItem item))
            {
                CustomWeaponComponent? cwc = item.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponPostAmmoPackContext(__instance));
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

        private static bool _allowReload = true;
        [HarmonyPatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.TriggerReload))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool ReloadPreStartCallback(PlayerInventoryLocal __instance)
        {
            _allowReload = true;
            CustomWeaponComponent? cwc = __instance.m_wieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return true;

            _allowReload = cwc.Invoke(new WeaponPreReloadContext()).Allow;
            return _allowReload;
        }

        [HarmonyPatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.TriggerReload))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ReloadStartCallback(PlayerInventoryLocal __instance)
        {
            if (!_allowReload) return;

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
