using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Heal
{
    internal sealed class HealFXSync : SyncedEvent<float>
    {
        public override string GUID => "HEALFX";

        protected override void Receive(float damage)
        {
            HealManager.Internal_ReceiveHealDamage(damage);
        }
    }
}