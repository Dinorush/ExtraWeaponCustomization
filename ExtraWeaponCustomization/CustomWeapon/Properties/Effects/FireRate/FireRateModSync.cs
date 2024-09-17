using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.FireRate
{
    internal sealed class FireRateModSync : SyncedEvent<TriggerInstanceData>
    {
        public override string GUID => "FRMOD";

        protected override void Receive(TriggerInstanceData packet)
        {
            if (!packet.source.TryGetPlayer(out var player)) return;

            FireRateModManager.Internal_ReceiveInstance(
                player,
                packet.slot,
                packet.mod.Get(FireRateModManager.MaxMod)
                );
        }
    }
}
