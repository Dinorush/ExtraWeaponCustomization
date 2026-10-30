using EWC.CustomWeapon.Enums;
using Player;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.Structs
{
    public struct pCWC
    {
        private SNetStructs.pReplicator pRep;
        
        public bool isSentry;
        public InventorySlot slot;

        public readonly bool IsValid => pRep.IsValid();

        public readonly ushort RepKeyPlusOne => pRep.keyPlusOne;

        public void Set(CustomWeaponComponent cwc)
        {
            isSentry = cwc.Owner.IsType(OwnerType.Sentry);
            slot = cwc.Weapon.InventorySlot;
            pRep.keyPlusOne = (ushort) (cwc.Owner.ReplicatorKey + 1);
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

        public readonly bool TryGet([MaybeNullWhen(false)] out CustomWeaponComponent comp)
        {
            if (!TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            if (isSentry)
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

        public readonly bool TryGetSentry([MaybeNullWhen(false)] out SentryGunInstance comp)
        {
            if (!isSentry || !TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            comp = supplier.Cast<SentryGunInstance>();
            return true;
        }

        public readonly bool TryGetPlayer([MaybeNullWhen(false)] out PlayerAgent comp)
        {
            if (isSentry || !TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            comp = supplier.Cast<PlayerSync>().m_agent;
            return true;
        }

        public readonly bool TryGetSource([MaybeNullWhen(false)] out PlayerAgent comp)
        {
            if (!TryGetSupplier(out var supplier))
            {
                comp = null;
                return false;
            }

            if (!isSentry)
            {
                comp = supplier.Cast<PlayerSync>().m_agent;
                return true;
            }
            else
            {
                comp = supplier.Cast<SentryGunInstance>().Owner;
                return true;
            }
        }
    }
}
