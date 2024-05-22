using ExtraWeaponCustomization.Networking;
using Player;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    internal sealed class HealSync : SyncedEventMasterOnly<HealData>
    {
        public override string GUID => "EXPHEAL";

        protected override void Receive(HealData packet)
        {
            if (!packet.player.TryGet(out PlayerAgent player)) return;
            HealManager.Internal_ReceiveHeal(
                player,
                packet.heal.Get(player.Damage.HealthMax),
                packet.cap.Get(player.Damage.HealthMax)
                );
        }
    }
}