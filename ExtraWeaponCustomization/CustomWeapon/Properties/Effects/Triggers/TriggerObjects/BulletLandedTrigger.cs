using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BulletLandedTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.BulletLanded;
        public float Amount { get; private set; } = 1f;

        public BulletLandedTrigger() {}

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            // Want to trigger when a bullet lands but NOT on a pre-hit context
            if (context is WeaponHitContextBase && context is not WeaponPreHitDamageableContext)
            {
                amount = Amount;
                return true;
            }
            return false;
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
            }
        }
    }
}
