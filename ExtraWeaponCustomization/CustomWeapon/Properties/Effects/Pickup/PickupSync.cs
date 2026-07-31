using Agents;
using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Pickup
{
    internal sealed class PickupSync : SyncedEvent<PickupData>
    {
        public override string GUID => "PICKUP";

        protected override void Receive(PickupData data)
        {
            if (!data.cwc.TryGet(out var cwc)) return;

            PickupManager.Internal_ReceivePickup(cwc);
        }
    }
}