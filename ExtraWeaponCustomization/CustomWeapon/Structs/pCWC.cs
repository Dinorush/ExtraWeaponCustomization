using EWC.CustomWeapon.ComponentWrapper.OwnerComps;
using EWC.CustomWeapon.Enums;
using Player;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.Structs
{
    public struct pCWC
    {
        private SNetStructs.pReplicator pRep;
        
        public OwnerType ownerType;
        public InventorySlot slot;

        public bool IsValid => pRep.IsValid();

        public readonly ushort ReplicatorKey => pRep.keyPlusOne;

        public void Set(CustomWeaponComponent cwc)
        {
            ownerType = cwc.Owner.Type;
            slot = cwc.Weapon.InventorySlot;
            if (ownerType.HasFlag(OwnerType.Sentry))
                pRep.SetID(((SentryOwnerComp)cwc.Owner).Value.Replicator);
            else
                pRep.keyPlusOne = (ushort) (cwc.Owner.Player!.m_replicator.Key + 1);
        }

        private bool TryGetSupplier([MaybeNullWhen(false)] out IReplicatorSupplier supplier)
        {
            if (!pRep.TryGetID(out var rep) || rep == null)
            {
                supplier = null;
                return false;
            }

            supplier = rep.ReplicatorSupplier;
            return supplier != null;
        }

        public bool TryGet([MaybeNullWhen(false)] out CustomWeaponComponent comp)
        {
            if (!TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            if (ownerType.HasFlag(OwnerType.Sentry))
            {
                comp = supplier.Cast<SentryGunInstance>().GetComponent<CustomWeaponComponent>();
                return comp != null;
            }
            else
            {
                var player = supplier.Cast<PlayerSync>().m_agent;
                if (!PlayerBackpackManager.TryGetBackpack(player.Owner, out var backpack) || !backpack.TryGetBackpackItem(slot, out var bpItem))
                {
                    comp = null;
                    return false;
                }

                comp = bpItem.Instance.GetComponent<CustomWeaponComponent>();
                return comp != null;
            }
        }

        public bool TryGetSentry([MaybeNullWhen(false)] out SentryGunInstance comp)
        {
            if (!ownerType.HasFlag(OwnerType.Sentry) || !TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            comp = supplier.Cast<SentryGunInstance>();
            return true;
        }

        public bool TryGetPlayer([MaybeNullWhen(false)] out PlayerAgent comp)
        {
            if (!ownerType.HasFlag(OwnerType.Player) || !TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            comp = supplier.Cast<PlayerSync>().m_agent;
            return true;
        }
    }
}
