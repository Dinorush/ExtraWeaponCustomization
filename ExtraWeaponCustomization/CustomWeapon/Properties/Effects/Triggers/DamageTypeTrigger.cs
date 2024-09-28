using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageTypeTrigger<TContext> : IDamageTypeTrigger where TContext : WeaponDamageTypeContext
    {
        public DamageType DamageType { get; set; }
        public DamageType BlacklistType { get; set; }
        public TriggerName Name { get; }

        public DamageTypeTrigger(TriggerName name, DamageType type = DamageType.Any, DamageType blacklistType = DamageType.Invalid)
        {
            Name = name;
            DamageType = type;
            BlacklistType = blacklistType;
        }

        public virtual float Invoke(WeaponTriggerContext context)
        {
            return context is TContext hitContext
                && !hitContext.DamageType.HasFlag(BlacklistType)
                && hitContext.DamageType.HasFlag(DamageType) ? 1f : 0f;
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch(property)
            {
                case "damagetype":
                case "type":
                    DamageType = IDamageTypeTrigger.ResolveDamageType(reader.GetString());
                    break;
            }
        }
    }
}
