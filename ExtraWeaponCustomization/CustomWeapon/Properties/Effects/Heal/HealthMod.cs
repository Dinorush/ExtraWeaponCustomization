using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Player;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class HealthMod :
        IWeaponProperty<WeaponTriggerContext>
    {
        public readonly static string Name = typeof(HealthMod).Name;
        public bool AllowStack { get; } = true;

        public float HealthChange { get; set; } = 0f;
        public float HealthRelChange { get; set; } = 0f;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;

        public void Invoke(WeaponTriggerContext context)
        {
            if (!context.Type.IsType(TriggerType)) return;

            PlayerAgent owner = context.Weapon.Owner;
            owner.Damage.AddHealth(HealthChange + HealthRelChange * owner.Damage.HealthMax, owner);
        }

        public IWeaponProperty Clone()
        {
            HealthMod copy = new()
            {
                HealthChange = HealthChange,
                HealthRelChange = HealthRelChange,
                TriggerType = TriggerType
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(HealthChange), HealthChange);
            writer.WriteNumber(nameof(HealthRelChange), HealthRelChange);
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "healthchange":
                case "health":
                case "heal":
                    HealthChange = reader.GetSingle();
                    break;
                case "healthrelchange":
                case "healthrel":
                case "healrel":
                    HealthRelChange = reader.GetSingle();
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
