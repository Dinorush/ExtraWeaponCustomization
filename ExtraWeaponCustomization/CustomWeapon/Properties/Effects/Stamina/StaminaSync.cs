using EWC.Networking;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.Stamina
{
    internal sealed class StaminaSync : SyncedEvent<StaminaData>
    {
        public override string GUID => "STAM";

        protected override void Receive(StaminaData packet)
        {
            StaminaManager.Internal_ReceiveStamina(
                packet.mod.Get(1f),
                packet.cap.Get(1f),
                packet.cancelRegen
                );
        }
    }
}