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

        public virtual float Invoke(WeaponTriggerContext context)
        {
            return context is TContext hitContext
                && !hitContext.DamageType.HasAnyFlag(BlacklistType)
                && hitContext.DamageType.HasFlag(DamageType) ? Amount : 0f;
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
