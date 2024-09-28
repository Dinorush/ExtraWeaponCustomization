using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    internal sealed class EWCProjectileSyncDestroy : SyncedEvent<ProjectileDataDestroy>
    {
        public override string GUID => "PROJDST";

        protected override void Receive(ProjectileDataDestroy packet)
        {
            EWCProjectileManager.Internal_ReceiveProjectileDestroy(packet.id);
        }
    }
}