using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Structs;
using Player;
using SNetwork;

namespace EWC.CustomWeapon.Properties.Effects.Pickup
{
    public static class PickupManager
    {
        private readonly static PickupSync _sync = new();

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
        }

        public static void DoPickup(CustomWeaponComponent cwc)
        {
            if (!cwc.Weapon.IsAnyType(Enums.WeaponType.SentryHolder | Enums.WeaponType.Sentry)) return;

            PickupData data = new();
            data.cwc.Set(cwc);
            _sync.Send(data, SNet.Master);
        }

        internal static void Internal_ReceivePickup(CustomWeaponComponent cwc)
        {
            if (cwc.Weapon.IsType(Enums.WeaponType.Sentry))
            {
                PickupSentry(((SentryGunComp)cwc.Weapon).Value, cwc);
            }
            else if (CustomWeaponManager.TryGetSentry(cwc.Owner.Player!, out var sentryInfo))
            {
                PickupSentry(sentryInfo.sentry, cwc);
            }
        }

        // Want to use SyncedPickup, but don't want to equip sentry, so need to run a custom SetDeployed first
        private static void PickupSentry(SentryGunInstance instance, CustomWeaponComponent cwc)
        {
            if (SNet.IsMaster)
            {
                if (instance.Alive)
                {
                    PickupData data = new();
                    data.cwc.Set(cwc);
                    _sync.Send(data);
                }
                else
                    return;
            }

            var player = instance.Owner;
            var snetPlayer = player.Owner;
            var bp = PlayerBackpackManager.GetBackpack(snetPlayer);
            if (bp.TryGetBackpackItem(InventorySlot.GearClass, out var bpItem) && bpItem.Status != eInventoryItemStatus.InBackpack)
            {
                bpItem.Status = eInventoryItemStatus.InBackpack;
                if (cwc.Owner.IsType(Enums.OwnerType.Managed))
                {
                    pInventoryItemStatus data = new();
                    data.sourcePlayer.SetPlayer(snetPlayer);
                    data.slot = InventorySlot.GearClass;
                    data.status = eInventoryItemStatus.InBackpack;
                    PlayerBackpackManager.InventoryItemStatusChange(data);
                }
                else
                {
                    bp.ShowHideInstance(bpItem, visible: true);
                }
                GuiManager.PlayerLayer.Inventory.UpdateItemUI(bpItem, player.Inventory.WieldedSlot);
            }
            instance.SyncedPickup(player);
        }
    }

    public struct PickupData
    {
        public pCWC cwc;
    }
}
