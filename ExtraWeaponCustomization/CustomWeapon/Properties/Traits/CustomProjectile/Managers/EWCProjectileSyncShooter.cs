using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    internal sealed class EWCProjectileSyncShooter : SyncedEvent<ProjectileDataShooter>
    {
        public override string GUID => "PROJSHT";

        protected override void Receive(ProjectileDataShooter packet)
        {
            EWCProjectileManager.Shooter.Internal_ReceiveProjectile(
                packet.characterIndex,
                packet.id,
                packet.type,
                packet.position,
                packet.velocity,
                packet.gravity.Get(EWCProjectileManager.MaxGravity)
                );
        }
    }
}