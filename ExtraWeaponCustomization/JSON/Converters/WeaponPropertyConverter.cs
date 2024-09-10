using ExtraWeaponCustomization.CustomWeapon.Properties;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExtraWeaponCustomization.JSON.Converters
{
    public sealed class WeaponPropertyConverter : JsonConverter<IWeaponProperty>
    {
        private static readonly string PropertyNamespace = typeof(IWeaponProperty).Namespace!;
        public override IWeaponProperty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            IWeaponProperty? instance = CreatePropertyInstance(reader);
            if (instance == null) return null;

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

        public override void Write(Utf8JsonWriter writer, IWeaponProperty value, JsonSerializerOptions options)
        {
            value.Serialize(writer);
        }

        private static IWeaponProperty? CreatePropertyInstance(Utf8JsonReader reader)
        {
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
                name = name.Replace(" ", "");

                Type? type = Type.GetType(PropertyNamespace + ".Effects." + name, false, true) ?? Type.GetType(PropertyNamespace + ".Traits." + name, false, true);
                if (type == null) throw new JsonException("Unable to find corresponding weapon property for \"" + name + "\"");

                return (IWeaponProperty?)Activator.CreateInstance(type);
            }

            return null;
        }
    }
}
