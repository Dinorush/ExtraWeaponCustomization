using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Player;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class HealthMod :
        IWeaponProperty<WeaponTriggerContext>
    {
        public readonly static string Name = typeof(HealthMod).Name;
        public bool AllowStack { get; } = true;

        public float HealthChangeRel { get; set; } = 0f;
        public float CapRel { get; set; } = -1f;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;

        public void Invoke(WeaponTriggerContext context)
        {
            if (!context.Type.IsType(TriggerType)) return;

            float cap = CapRel >= 0f ? CapRel : Math.Sign(HealthChangeRel);
            PlayerAgent owner = context.Weapon.Owner;
            HealManager.DoHeal(
                owner,
                HealthChangeRel * owner.Damage.HealthMax,
                cap * owner.Damage.HealthMax
                );
        }

        public IWeaponProperty Clone()
        {
            HealthMod copy = new()
            {
                HealthChangeRel = HealthChangeRel,
                CapRel = CapRel,
                TriggerType = TriggerType
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(HealthChangeRel), HealthChangeRel);
            writer.WriteNumber(nameof(CapRel), CapRel);
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "healthchangerel":
                case "healthchange":
                case "healthrel":
                case "health":
                case "healrel":
                case "heal":
                    HealthChangeRel = reader.GetSingle();
                    break;
                case "caprel":
                case "cap":
                    CapRel = reader.GetSingle();
                    break;
                case "triggertype":
                case "trigger":
                    TriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    break;
                default:
                    break;
            }
        }
    }
}
