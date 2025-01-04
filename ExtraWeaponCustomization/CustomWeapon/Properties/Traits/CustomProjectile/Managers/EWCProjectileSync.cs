using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    internal sealed class EWCProjectileSyncDestroy : SyncedEvent<ProjectileDataDestroy>
    {
        public override string GUID => "PROJDST";

        protected override void Receive(ProjectileDataDestroy packet)
        {
            EWCProjectileManager.Internal_ReceiveProjectileDestroy(packet.playerIndex, packet.id);
        }
    }

    internal sealed class EWCProjectileSyncTarget : SyncedEvent<ProjectileDataTarget>
    {
        public override string GUID => "PROJTGT";

        protected override void Receive(ProjectileDataTarget packet)
        {
            if (!packet.target.TryGet(out var agent))
                agent = null;

            EWCProjectileManager.Internal_ReceiveProjectileTarget(packet.playerIndex, packet.id, agent, packet.limbID);
        }
    }

    internal sealed class EWCProjectileSyncShooter : SyncedEvent<ProjectileDataShooter>
    {
        public override string GUID => "PROJSHT";

        protected override void Receive(ProjectileDataShooter packet)
        {
            if (!CustomWeaponManager.TryGetSyncProperty<Projectile>(packet.propertyID, out var property)) return;

            EWCProjectileManager.Shooter.Internal_ReceiveProjectile(
                packet.playerIndex,
                packet.id,
                property,
                packet.position,
                packet.dir.Value
                );
        }
    }
}
