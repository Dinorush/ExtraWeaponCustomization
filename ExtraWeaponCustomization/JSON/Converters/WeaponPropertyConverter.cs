﻿using EWC.CustomWeapon.Properties;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public sealed class WeaponPropertyConverter : JsonConverter<WeaponPropertyBase>
    {
        private static readonly string PropertyNamespace = typeof(WeaponPropertyBase).Namespace!;
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

            while (reader.Read())
            {
                // Our classes do not contain any objects within themselves. For now, just returning early if any are found.
                if (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.StartObject)
                {
                    throw new JsonException("Weapon property does not contain Name field.");
                }

                if (reader.TokenType != JsonTokenType.PropertyName) continue;

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
            // Reference Property is not an effect or trait so it can't be dereferenced
            if (name.ToLower() == nameof(ReferenceProperty).ToLower()) return new ReferenceProperty();

            Type? type = Type.GetType(PropertyNamespace + ".Effects." + name, false, true) ?? Type.GetType(PropertyNamespace + ".Traits." + name, false, true);
            if (type == null) return null;

            return (WeaponPropertyBase?)Activator.CreateInstance(type);
        }
    }
}
