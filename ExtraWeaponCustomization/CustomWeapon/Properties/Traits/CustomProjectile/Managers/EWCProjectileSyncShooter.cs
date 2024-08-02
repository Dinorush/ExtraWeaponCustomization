using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    internal sealed class EWCProjectileSyncShooter : SyncedEvent<ProjectileDataShooter>
    {
        public override string GUID => "PROJSHT";

        protected override void Receive(ProjectileDataShooter packet)
        {
            EWCProjectileManager.Shooter.Internal_ReceiveProjectile(
                packet.id,
                packet.type,
                packet.position,
                packet.dir.Value * packet.speed.Get(EWCProjectileManager.MaxSpeed),
                packet.gravity.Get(EWCProjectileManager.MaxGravity),
                packet.scale.Get(EWCProjectileManager.MaxScale),
                packet.glowColor,
                packet.glowRange.Get(EWCProjectileManager.MaxGlowRange)
                );
        }
    }
}