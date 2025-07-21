using EWC.Networking;
using Enemies;
using Player;

namespace EWC.CustomWeapon.Properties.Effects.Hit.Explosion
{
    internal sealed class ExplosionDamageSync : SyncedEventMasterOnly<ExplosionDamageData>
    {
        public override string GUID => "EXPDMG";

        protected override void Receive(ExplosionDamageData packet)
        {
            if (!packet.target.TryGet(out EnemyAgent target)) return;
            packet.source.TryGet(out PlayerAgent? source);

            ExplosionManager.Internal_ReceiveExplosionDamage(
                target,
                source,
                packet.limbID,
                packet.damageLimb,
                packet.localPosition.Get(10f),
                packet.damage,
                packet.staggerMult
                );
        }
    }

    internal sealed class ExplosionDamagePlayerSync : SyncedEvent<ExplosionDamagePlayerData>
    {
        public override string GUID => "EXPDMGP";

        protected override void Receive(ExplosionDamagePlayerData packet)
        {
            if (!packet.target.TryGet(out PlayerAgent target)) return;
            packet.source.TryGet(out PlayerAgent source);

            ExplosionManager.Internal_ReceiveExplosionDamagePlayer(
                target,
                source,
                packet.damage,
                packet.direction.Value
                );
        }

        protected override void ReceiveLocal(ExplosionDamagePlayerData packet) => Receive(packet);
    }
}
