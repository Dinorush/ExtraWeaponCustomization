using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageFlagTrigger<TContext> : IDamageFlagTrigger where TContext : WeaponDamageFlagContext
    {
        public DamageFlag Type { get; set; }
        public DamageFlag BlacklistType { get; set; }
        public string Name { get; }

        public DamageFlagTrigger(string name, DamageFlag type = DamageFlag.Any, DamageFlag blacklistType = DamageFlag.Invalid)
        {
            Name = name;
            Type = type;
            BlacklistType = blacklistType;
        }

        public virtual float Invoke(WeaponTriggerContext context)
        {
            return context is TContext hitContext
                && !hitContext.DamageFlag.HasFlag(BlacklistType)
                && hitContext.DamageFlag.HasFlag(Type) ? 1f : 0f;
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch(property)
            {
                case "damageflag":
                case "damagetype":
                case "flag":
                case "type":
                    Type = IDamageFlagTrigger.ResolveDamageFlags(reader.GetString());
                    break;
            }
        }
    }
}
