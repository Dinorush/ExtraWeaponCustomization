using EWC.Networking;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.PlayerPush
{
    internal sealed class PushSync : SyncedEvent<PushData>
    {
        public override string GUID => "PUSH";

        protected override void Receive(PushData packet)
        {
            if (!PlayerManager.HasLocalPlayerAgent() || !CustomDataManager.TryGetSyncProperty<Push>(packet.propertyID, out var settings)) return;

            PushManager.Internal_ReceivePush(
                packet.force,
                settings
                );
        }
    }
}