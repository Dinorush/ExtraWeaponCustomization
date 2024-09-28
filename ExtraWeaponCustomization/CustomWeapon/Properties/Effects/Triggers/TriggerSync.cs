using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    internal sealed class TriggerSync : SyncedEvent<TriggerInstanceData>
    {
        public override string GUID => "TRGIN";

        protected override void Receive(TriggerInstanceData packet)
        {
            if (!packet.source.TryGetPlayer(out var player)) return;

            TriggerManager.Internal_ReceiveInstance(
                player,
                packet.slot,
                packet.mod.Get(TriggerManager.MaxMod),
                packet.id
                );
        }
    }

    internal sealed class TriggerResetSync : SyncedEvent<TriggerResetData>
    {
        public override string GUID => "TRGRS";

        protected override void Receive(TriggerResetData packet)
        {
            if (!packet.source.TryGetPlayer(out var player)) return;

            TriggerManager.Internal_ReceiveReset(
                player,
                packet.slot,
                packet.id
                );
        }
    }
}
