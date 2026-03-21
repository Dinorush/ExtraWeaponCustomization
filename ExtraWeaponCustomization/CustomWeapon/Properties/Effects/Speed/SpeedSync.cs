using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Speed
{
    internal sealed class SpeedSync : SyncedEvent<SpeedData>
    {
        public override string GUID => "SPEED";

        protected override void Receive(SpeedData packet)
        {
            SpeedManager.Internal_ReceiveSpeed(packet);
        }
    }
}