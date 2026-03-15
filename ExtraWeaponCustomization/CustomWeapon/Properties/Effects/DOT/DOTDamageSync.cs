using Enemies;
using EWC.Networking;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT
{
    internal sealed class DOTDamageEnemySync : SyncedEventMasterOnly<DOTData>
    {
        public override string GUID => "DOT";

        protected override void Receive(DOTData packet)
        {
            if (!packet.target.TryGet(out EnemyAgent enemy)) return;
            packet.cwc.TryGetSource(out PlayerAgent? player);

            DOTDamageManager.Internal_ReceiveDOTDamageEnemy(
                enemy,
                player,
                packet.cwc.ownerType,
                packet.limbID,
                packet.damageLimb,
                packet.localPosition.Get(10f),
                packet.damage,
                packet.staggerMult,
                packet.setCooldowns
                );
        }
    }

    internal sealed class DOTDamagePlayerSync : SyncedEvent<DOTPlayerData>
    {
        public override string GUID => "DOTP";

        protected override void Receive(DOTPlayerData packet)
        {
            if (!packet.target.TryGet(out PlayerAgent target)) return;

            DOTDamageManager.Internal_ReceiveDOTDamagePlayer(
                target,
                packet.damage
                );
        }

        protected override void ReceiveLocal(DOTPlayerData packet) => Receive(packet);
    }
}
