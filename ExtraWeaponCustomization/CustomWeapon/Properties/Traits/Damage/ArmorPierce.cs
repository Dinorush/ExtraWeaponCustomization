using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class ArmorPierce :
        Trait,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponArmorContext>
    {
        public float Pierce { get; private set; } = 1f;
        public PierceType Type { get; private set; } = PierceType.Pierce;

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

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Pierce), Pierce);
            writer.WriteString(nameof(Type), Type.ToString());
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
