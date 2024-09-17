using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.TempProp
{
    internal sealed class TempPropertiesSync : SyncedEvent<TriggerData>
    {
        public override string GUID => "TEMP";

        protected override void Receive(TriggerData packet)
        {
            if (!packet.source.TryGetPlayer(out var player)) return;

            TempPropertiesManager.Internal_ReceiveInstance(
                player,
                packet.slot
                );
        }
    }
}
