using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
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
                packet.accel.Get(EWCProjectileManager.MaxAccel),
                packet.accelExpo.Get(EWCProjectileManager.MaxAccelExpo),
                packet.accelTime.Get(EWCProjectileManager.MaxAccelTime),
                packet.gravity.Get(EWCProjectileManager.MaxGravity),
                packet.scale.Get(EWCProjectileManager.MaxScale),
                packet.trail,
                packet.glowColor,
                packet.glowRange.Get(EWCProjectileManager.MaxGlowRange),
                packet.lifetime.Get(EWCProjectileManager.MaxLifetime)
                );
        }
    }
}