using ExtraWeaponCustomization.Networking;
using Player;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    internal sealed class HealDamageSync : SyncedEventMasterOnly<HealData>
    {
        public override string GUID => "EXPHEAL";

        protected override void Receive(HealData packet)
        {
            if (!packet.player.TryGet(out PlayerAgent player)) return;
            HealManager.Internal_ReceiveHealDamage(player, packet.healDamage.Get(player.Damage.HealthMax));
        }
    }
}