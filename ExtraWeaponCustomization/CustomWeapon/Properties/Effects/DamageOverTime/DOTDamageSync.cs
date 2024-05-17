using Enemies;
using ExtraWeaponCustomization.Networking;
using Player;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    internal sealed class DOTDamageSync : SyncedEvent<DOTData>
    {
        public override string GUID => "DOT";

        protected override void Receive(DOTData packet)
        {
            if (!packet.target.TryGet(out EnemyAgent enemy)) return;
            packet.source.TryGet(out PlayerAgent? player);

            DOTDamageManager.Internal_ReceiveDOTDamage(
                enemy,
                player,
                packet.limbID,
                packet.localPosition.Get(10f),
                packet.damage.Get(enemy.Damage.DamageMax),
                packet.staggerMult.Get(DOTDamageManager.MaxStagger)
                );
        }
    }
}
