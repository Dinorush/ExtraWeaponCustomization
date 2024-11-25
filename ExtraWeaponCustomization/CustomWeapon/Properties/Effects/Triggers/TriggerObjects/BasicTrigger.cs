using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BasicTrigger<TContext> : ITrigger where TContext : WeaponTriggerContext
    {
        public TriggerName Name { get; }
        public float Amount { get; private set; } = 1f;

        public BasicTrigger(TriggerName name)
        {
            Name = name;
        }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is TContext)
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
