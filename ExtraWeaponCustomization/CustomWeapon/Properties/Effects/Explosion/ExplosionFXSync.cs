using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Hit.Explosion
{
    internal sealed class ExplosionFXSync : SyncedEvent<ExplosionFXData>
    {
        public override string GUID => "EXPFX";

        protected override void Receive(ExplosionFXData packet)
        {
            if (!CustomDataManager.TryGetSyncProperty<Explosive>(packet.propertyID, out var property)) return;

            ExplosionFXManager.Internal_ReceiveExplosionFX(
                packet.position,
                packet.normal.Value,
                property
                );
        }

        protected override void ReceiveLocal(ExplosionFXData packet)
        {
            Receive(packet);
        }
    }
}