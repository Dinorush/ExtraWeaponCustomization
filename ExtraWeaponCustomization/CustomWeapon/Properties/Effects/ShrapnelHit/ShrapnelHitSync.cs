using Enemies;
using EWC.Networking;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.ShrapnelHit
{
    internal sealed class ShrapnelHitSync : SyncedEventMasterOnly<ShrapnelHitData>
    {
        public override string GUID => "SHPHIT";

        protected override void Receive(ShrapnelHitData packet)
        {
            if (!packet.target.TryGet(out EnemyAgent enemy)) return;
            packet.cwc.TryGetPlayer(out PlayerAgent? player);

            ShrapnelHitManager.Internal_ReceiveShrapnelDamage(
                enemy,
                player,
                packet.cwc.ownerType,
                packet.limbID,
                packet.damageLimb,
                packet.localPosition.Get(10f),
                packet.dir.Value,
                packet.damage,
                packet.staggerMult,
                packet.setCooldowns
                );
        }
    }
}
