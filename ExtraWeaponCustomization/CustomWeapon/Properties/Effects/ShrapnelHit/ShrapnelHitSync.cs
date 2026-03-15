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
            packet.cwc.TryGetSource(out PlayerAgent? player);

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

    internal sealed class ShrapnelHitPlayerSync : SyncedEvent<ShrapnelHitPlayerData>
    {
        public override string GUID => "SHPHITP";

        protected override void Receive(ShrapnelHitPlayerData packet)
        {
            if (!packet.target.TryGet(out PlayerAgent target)) return;
            packet.cwc.TryGetSource(out PlayerAgent? player);

            ShrapnelHitManager.Internal_ReceiveShrapnelDamagePlayer(
                target,
                player,
                packet.cwc.ownerType,
                packet.damage,
                packet.dir.Value
                );
        }

        protected override void ReceiveLocal(ShrapnelHitPlayerData packet) => Receive(packet);
    }
}
