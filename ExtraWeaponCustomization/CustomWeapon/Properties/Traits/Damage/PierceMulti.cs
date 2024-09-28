using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class PierceMulti : 
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPierceContext>
    {
        public float PierceDamageMulti { get; set; } = 1f;

        public void Invoke(WeaponPierceContext context)
        {
            context.AddMod(PierceDamageMulti, StackType.Multiply);
        }

        public override IWeaponProperty Clone()
        {
            PierceMulti copy = new()
            {
                PierceDamageMulti = PierceDamageMulti
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(PierceDamageMulti), PierceDamageMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "piercedamagemulti":
                case "piercemulti":
                case "multi":
                    PierceDamageMulti = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
