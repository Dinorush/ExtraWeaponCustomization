﻿using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class BackstabMulti : 
        Trait,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponBackstabContext>
    {
        public float BackstabDamageMulti { get; private set; } = 1f;

        public void Invoke(WeaponBackstabContext context)
        {
            context.AddMod(BackstabDamageMulti, StackType.Multiply);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(BackstabDamageMulti), BackstabDamageMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property.ToLowerInvariant())
            {
                case "backstabdamagemulti":
                case "backdamagemulti":
                case "backstabmulti":
                case "backmulti":
                case "multi":
                    BackstabDamageMulti = reader.GetSingle();
                    break;
            }
        }
    }
}
