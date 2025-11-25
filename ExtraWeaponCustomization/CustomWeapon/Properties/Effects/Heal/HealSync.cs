using EWC.Networking;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.Heal
{
    internal sealed class HealSync : SyncedEventMasterOnly<HealData>
    {
        public override string GUID => "HEAL";

        protected override void Receive(HealData packet)
        {
            if (!packet.player.TryGet(out PlayerAgent player)) return;
            HealManager.Internal_ReceiveHeal(
                player,
                packet.heal.Get(player.Damage.HealthMax),
                packet.cap.Get(player.Damage.HealthMax),
                packet.cancelRegen
                );
        }
    }
}