using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Pickup
{
    internal sealed class PickupSync : SyncedEvent<PickupData>
    {
        public override string GUID => "PICKUP";

        protected override void Receive(PickupData data)
        {
            if (!data.bpItem.TryGet(out var item)) return;

            PickupManager.Internal_ReceivePickup(item, data.ammo);
        }
    }
}