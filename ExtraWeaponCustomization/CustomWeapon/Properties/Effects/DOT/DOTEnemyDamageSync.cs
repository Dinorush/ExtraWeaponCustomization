using Enemies;
using EWC.Networking;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT
{
    internal sealed class DOTEnemyDamageSync : SyncedEventMasterOnly<DOTData>
    {
        public override string GUID => "DOT";

        protected override void Receive(DOTData packet)
        {
            if (!packet.target.TryGet(out EnemyAgent enemy)) return;
            packet.source.TryGet(out PlayerAgent? player);

            DOTDamageManager.Internal_ReceiveDOTEnemyDamage(
                enemy,
                player,
                packet.limbID,
                packet.localPosition.Get(10f),
                packet.damage.Get(enemy.Damage.DamageMax),
                packet.staggerMult.Get(DOTDamageManager.MaxStagger),
                packet.setCooldowns
                );
        }
    }
}
