using EWC.Attributes;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Networking.Structs;
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
            PickupData data = new();
            if (cwc.Weapon.IsType(Enums.WeaponType.Sentry))
            {
                var player = cwc.Owner.Player;
                if (player == null || !data.bpItem.TrySet(player.Owner, InventorySlot.GearClass))
                    return;
            }
            else 
            {
                if (!cwc.Weapon.IsType(Enums.WeaponType.SentryHolder))
                    return;
                data.bpItem.Set(cwc.Weapon.Component);
            }

            _sync.Send(data, SNet.Master, SNet_ChannelType.GameOrderCritical);
        }

        internal static void Internal_ReceivePickup(ItemEquippable item, float ammo)
        {
            if (!SNet.IsMaster)
            {
                PickupSentry(item, ammo);
                return;
            }

            if (!CustomWeaponManager.TryGetSentry(item.Owner, out var sentryInfo) || !sentryInfo.sentry.Alive)
                return;

            PickupData data = new();
            data.bpItem.Set(item);
            data.ammo = sentryInfo.sentry.Ammo;
            _sync.Send(data, priority: SNet_ChannelType.GameOrderCritical);

            PickupSentry(item, data.ammo);
            ItemReplicationManager.DeSpawn(sentryInfo.sentry.Replicator);
        }

        // Want to use SyncedPickup, but don't want to equip sentry, so need to run a custom SetDeployed first
        private static void PickupSentry(ItemEquippable item, float ammo)
        {
            var player = item.Owner;
            var snetPlayer = player.Owner;
            bool managed = snetPlayer.IsLocal || (snetPlayer.IsBot && SNet.IsMaster);
            var bp = PlayerBackpackManager.GetBackpack(snetPlayer);
            if (bp.TryGetBackpackItem(InventorySlot.GearClass, out var bpItem) && bpItem.Status != eInventoryItemStatus.InBackpack)
            {
                bpItem.Status = eInventoryItemStatus.InBackpack;
                if (managed)
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

                if (snetPlayer.IsLocal)
                {
                    var inventoryUI = GuiManager.PlayerLayer.Inventory;
                    inventoryUI.UpdateItemUI(bpItem, player.Inventory.WieldedSlot);
                }
                else
                {
                    var inventorySynced = player.Inventory.Cast<PlayerInventorySynced>();
                    inventorySynced.PlaceBackpackItem(bpItem, InventorySlot.GearClass);
                }

                if (snetPlayer.IsInSlot)
                {
                    var inventoryMap = GuiManager.MainMenuLayer.PageMap.m_inventory[snetPlayer.PlayerSlotIndex()];
                    inventoryMap.UpdateItemUI(bpItem, InventorySlot.None);
                }
            }

            if (managed)
                bp.AmmoStorage.UpdateAmmoInPack(AmmoType.Class, ammo);

            var cwc = item.GetComponent<CustomWeaponComponent>();
            cwc?.Invoke(new WeaponSentryStateContext(false));
        }
    }

    public struct PickupData
    {
        public pBackpackItem bpItem;
        public float ammo;
    }
}
