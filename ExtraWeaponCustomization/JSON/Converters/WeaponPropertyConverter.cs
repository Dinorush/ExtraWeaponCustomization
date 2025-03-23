using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public sealed class WeaponPropertyConverter : JsonConverter<WeaponPropertyBase>
    {
        private static readonly string PropertyNamespace = typeof(WeaponPropertyBase).Namespace!;
        private static readonly Dictionary<string, Type> LegacyNames = new()
        {
            ["damagemod"] = typeof(ShotMod),
            ["damagemodpertarget"] = typeof(ShotModPerTarget),
        };

        public override WeaponPropertyBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            WeaponPropertyBase? instance = CreatePropertyInstance(reader);
            if (instance == null || reader.TokenType != JsonTokenType.StartObject) return instance;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return instance;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");

                string property = reader.GetString()!;
                reader.Read();
                instance.DeserializeProperty(property.ToLowerInvariant().Replace(" ", ""), ref reader);
            }

            throw new JsonException("Expected EndObject token");
        }

        public override void Write(Utf8JsonWriter writer, WeaponPropertyBase value, JsonSerializerOptions options)
        {
            value.Serialize(writer);
        }

        private static WeaponPropertyBase? CreatePropertyInstance(Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String) return new ReferenceProperty(reader.GetString()!);
            if (reader.TokenType == JsonTokenType.Number) return new ReferenceProperty(reader.GetUInt32());

            if (reader.TokenType != JsonTokenType.StartObject) return null;

            int objCount = 1;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                    objCount++;

                if (reader.TokenType == JsonTokenType.EndObject && --objCount == 0)
                    throw new JsonException("Weapon property does not contain Name field.");

                if (objCount != 1 || reader.TokenType != JsonTokenType.PropertyName) continue;

                string property = reader.GetString()!;
                if (property.ToLowerInvariant() != "name") continue;


                reader.Read();
                string? name = reader.GetString();
                if (name == null) throw new JsonException("Name field cannot be empty in weapon property.");

                return NameToProperty(name);
            }

            return null;
        }

        private static WeaponPropertyBase? NameToProperty(string name)
        {
            name = name.Replace(" ", "");
            var lower = name.ToLower();
            // Reference Property is not an effect or trait so it can't be dereferenced
            if (lower == nameof(ReferenceProperty).ToLower()) return new ReferenceProperty();

            if (!LegacyNames.TryGetValue(lower, out Type? type))
                type = Type.GetType(PropertyNamespace + ".Effects." + name, false, true) ?? Type.GetType(PropertyNamespace + ".Traits." + name, false, true);

            if (type == null) throw new JsonException("Unable to find property with name " + name);

            return (WeaponPropertyBase?)Activator.CreateInstance(type);
        }
    }
}
