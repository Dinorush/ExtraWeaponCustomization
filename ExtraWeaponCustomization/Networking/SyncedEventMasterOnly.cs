using SNetwork;

namespace ExtraWeaponCustomization.Networking
{
    public abstract class SyncedEventMasterOnly<T> : SyncedEvent<T> where T : struct
    {
        public void Send(T packet, SNet_ChannelType priority = SNet_ChannelType.GameNonCritical)
        {
            if (!SNet.IsMaster)
                Send(packet, SNet.Master, priority);
            else
                Receive(packet);
        }
    }
}
