using Player;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EWC.Networking.Structs
{
    public struct pBackpackItem
    {
        private SNetStructs.pReplicator pRep;

        public InventorySlot slot;

        public readonly bool IsValid => pRep.IsValid();

        public readonly ushort RepKeyPlusOne => pRep.keyPlusOne;

        public bool TrySet(SNet_Player player, InventorySlot slot)
        {
            if (!PlayerBackpackManager.TryGetItem(player, InventorySlot.GearClass, out var bpItem))
                return false;

            Set(bpItem.Instance);
            return true;
        }

        public void Set(Item item)
        {
            slot = item.ItemDataBlock.inventorySlot;
            pRep.keyPlusOne = (ushort)(item.Owner.m_replicator.Key + 1);
        }

        private readonly bool TryGetSupplier([MaybeNullWhen(false)] out IReplicatorSupplier supplier)
        {
            if (!pRep.TryGetID(out var rep) || rep == null)
            {
                supplier = null;
                return false;
            }

            supplier = rep.ReplicatorSupplier;
            return supplier != null;
        }

        public readonly bool TryGet([MaybeNullWhen(false)] out ItemEquippable comp)
        {
            if (!TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            var player = supplier.Cast<PlayerSync>().m_agent;
            if (!PlayerBackpackManager.TryGetBackpack(player.Owner, out var backpack) || !backpack.TryGetBackpackItem(slot, out var bpItem))
            {
                comp = null;
                return false;
            }

            comp = bpItem.Instance.Cast<ItemEquippable>();
            return comp != null;
        }
    }
}
