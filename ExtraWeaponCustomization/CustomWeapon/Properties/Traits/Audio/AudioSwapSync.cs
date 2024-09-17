using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.Audio
{
    internal sealed class AudioSwapSync : SyncedEvent<TriggerData>
    {
        public override string GUID => "AUDIO";

        protected override void Receive(TriggerData packet)
        {
            if (!packet.source.TryGetPlayer(out var player)) return;

            AudioSwapManager.Internal_ReceiveInstance(
                player,
                packet.slot
                );
        }
    }
}
