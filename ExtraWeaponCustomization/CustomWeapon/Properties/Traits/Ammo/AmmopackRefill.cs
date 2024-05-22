using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public class AmmopackRefill : IWeaponProperty<WeaponPreAmmoPackContext>
    {
        public readonly static string Name = typeof(AmmopackRefill).Name;
        public bool AllowStack { get; } = false;

        public float AmmoRefillRel { get; set; } = 0.2f;

        public void Invoke(WeaponPreAmmoPackContext context)
        {
            context.AmmoRel = AmmoRefillRel;
        }

        public IWeaponProperty Clone()
        {
            AmmopackRefill copy = new()
            {
                AmmoRefillRel = AmmoRefillRel,
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(AmmoRefillRel), AmmoRefillRel);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "ammorefillrel":
                case "ammorefill":
                case "refillrel":
                case "refill":
                case "ammorel":
                case "ammo":
                    AmmoRefillRel = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
