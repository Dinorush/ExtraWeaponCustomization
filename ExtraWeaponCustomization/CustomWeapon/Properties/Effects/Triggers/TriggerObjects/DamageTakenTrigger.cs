using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class DamageTakenTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.DamageTaken;
        public float Amount { get; private set; } = 1f;
        public float Cap { get; private set; } = 0f;

        public DamageTakenTrigger() {}

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is WeaponDamageTakenContext damageContext)
            {
                amount = Cap > 0f ? Math.Min(Cap, Amount * damageContext.Damage) : Amount * damageContext.Damage;
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
                case "cap":
                    Cap = reader.GetSingle();
                    break;
            }
        }
    }
}
