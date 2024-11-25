using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageTypeTrigger<TContext> : IDamageTypeTrigger where TContext : WeaponDamageTypeContext
    {
        public DamageType DamageType { get; private set; }
        public DamageType BlacklistType { get; set; }
        public TriggerName Name { get; }
        public float Amount { get; private set; } = 1f;

        public DamageTypeTrigger(TriggerName name, DamageType type = DamageType.Any, DamageType blacklistType = DamageType.Any)
        {
            Name = name;
            DamageType = type;
            BlacklistType = blacklistType;
        }

        public virtual bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;

            if (context is TContext hitContext
                && !hitContext.DamageType.HasAnyFlag(BlacklistType)
                && hitContext.DamageType.HasFlag(DamageType))
            {
                amount = Amount;
                return true;
            }
            return false;
        }

        public virtual void Reset() { }

        public virtual ITrigger Clone() => this;
        protected void CloneValues(DamageTypeTrigger<TContext> trigger)
        {
            trigger.Amount = Amount;
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
