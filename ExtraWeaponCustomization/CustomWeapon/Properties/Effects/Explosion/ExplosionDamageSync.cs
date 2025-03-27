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
}
