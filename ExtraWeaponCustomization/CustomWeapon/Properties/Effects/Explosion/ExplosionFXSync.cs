using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects
{
    internal sealed class ExplosionFXSync : SyncedEvent<ExplosionFXData>
    {
        public override string GUID => "EXPFX";

        protected override void Receive(ExplosionFXData packet)
        {
            ExplosionFXManager.Internal_ReceiveExplosionFX(
                packet.position,
                packet.radius.Get(ExplosionManager.MaxRadius),
                packet.soundID,
                packet.color,
                packet.duration.Get(ExplosionFXManager.MaxGlowDuration),
                packet.fadeDuration.Get(ExplosionFXManager.MaxGlowDuration)
                );
        }

        protected override void ReceiveLocal(ExplosionFXData packet)
        {
            Receive(packet);
        }
    }
}