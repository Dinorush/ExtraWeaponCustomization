using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    internal class TumorMulti : IWeaponProperty<WeaponDamageContext>
    {
        public bool AllowStack { get; } = false;

        public float Multi { get; set; } = 1f;

        public void Invoke(WeaponDamageContext context)
        {
            Dam_EnemyDamageLimb_Custom? tumor = context.Damageable.TryCast<Dam_EnemyDamageLimb_Custom>();
            if (tumor != null)
                context.AddMod(Multi, StackType.Multiply);
        }

        public IWeaponProperty Clone()
        {
            TumorMulti copy = new()
            {
                Multi = Multi
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Multi), Multi);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "multiplier":
                case "multi":
                    Multi = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
