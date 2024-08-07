﻿using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class PierceMulti : 
        Trait,
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

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(PierceDamageMulti), PierceDamageMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
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
