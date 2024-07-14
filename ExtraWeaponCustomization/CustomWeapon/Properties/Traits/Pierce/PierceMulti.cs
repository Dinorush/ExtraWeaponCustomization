using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class PierceMulti : IWeaponProperty<WeaponPierceContext>
    {
        public bool AllowStack { get; } = false;
        public float PierceDamageMulti { get; set; } = 1f;

        public void Invoke(WeaponPierceContext context)
        {
            context.AddMod(PierceDamageMulti, StackType.Multiply);
        }

        public IWeaponProperty Clone()
        {
            PierceMulti copy = new()
            {
                PierceDamageMulti = PierceDamageMulti
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(PierceDamageMulti), PierceDamageMulti);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "piercedamagemulti":
                case "piercemulti":
                    PierceDamageMulti = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
