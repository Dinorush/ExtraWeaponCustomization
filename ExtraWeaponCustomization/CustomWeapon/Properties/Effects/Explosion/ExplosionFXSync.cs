using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    internal sealed class ExplosionFXSync : SyncedEvent<ExplosionFXData>
    {
        public override string GUID => "EXPFX";

        protected override void Receive(ExplosionFXData packet)
        {
            ExplosionManager.Internal_ReceiveExplosionFX(packet.position, packet.radius.Get(ExplosionManager.MaxRadius));
        }
    }
}