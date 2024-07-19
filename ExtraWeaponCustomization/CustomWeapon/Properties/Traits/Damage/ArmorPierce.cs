using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class ArmorPierce :
        Trait,
        IWeaponProperty<WeaponArmorContext>
    {
        public float Pierce { get; set; } = 1f;
        public PierceType Type { get; set; } = PierceType.Pierce;

        public void Invoke(WeaponArmorContext context)
        {
            context.ArmorMulti = Type switch
            {
                PierceType.Pierce => context.ArmorMulti + (1f - context.ArmorMulti) * Pierce,
                PierceType.Multi => context.ArmorMulti * Pierce,
                PierceType.Override => Pierce,
                _ => context.ArmorMulti
            };
        }

        public override IWeaponProperty Clone()
        {
            return new ArmorPierce();
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Pierce), Pierce);
            writer.WriteString(nameof(Type), Type.ToString());
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (property)
            {
                case "pierce":
                    Pierce = reader.GetSingle();
                    break;
                case "type":
                    Type = reader.GetString().ToEnum(PierceType.Pierce);
                    break;
            }
        }
    
        public enum PierceType
        {
            Pierce = 0,
            Multi = 1, Multiply = Multi,
            Override = 2
        }
    }
}
