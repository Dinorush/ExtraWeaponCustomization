﻿using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class PierceMulti : 
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPierceContext>
    {
        public float PierceDamageMulti { get; private set; } = 1f;

        public void Invoke(WeaponPierceContext context)
        {
            context.AddMod(PierceDamageMulti, StackType.Multiply);
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
            base.DeserializeProperty(property, ref reader);
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
