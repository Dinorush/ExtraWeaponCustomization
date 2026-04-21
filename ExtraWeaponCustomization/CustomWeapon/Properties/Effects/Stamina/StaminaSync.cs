using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Stamina
{
    internal sealed class StaminaSync : SyncedEvent<StaminaData>
    {
        public override string GUID => "STAMINA";

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