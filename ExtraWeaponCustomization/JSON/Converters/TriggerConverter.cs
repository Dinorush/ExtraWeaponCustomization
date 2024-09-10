using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExtraWeaponCustomization.JSON.Converters
{
    public sealed class TriggerConverter : JsonConverter<ITrigger>
    {
        public override ITrigger? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Simple trigger case (just one)
            if (reader.TokenType == JsonTokenType.String)
                return ITrigger.GetTrigger(reader.GetString());

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected Trigger to be either a string or object");

            // Full object case
            ITrigger? trigger = CreateTriggerInstance(reader);
            if (trigger == null) return null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return trigger;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");
                string property = reader.GetString()!;

                reader.Read();
                trigger.DeserializeProperty(property.ToLowerInvariant().Replace(" ", ""), ref reader);
            }

            throw new JsonException("Expected EndObject token");
        }

        // Only called for templates, so don't need logic for customized triggers
        public override void Write(Utf8JsonWriter writer, ITrigger? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.Name ?? "Invalid");
        }

        private static ITrigger? CreateTriggerInstance(Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject) return null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.StartObject)
                    throw new JsonException("Trigger object does not contain Name field.");

                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                string property = reader.GetString()!;
                if (property.ToLowerInvariant() != "name") continue;

                reader.Read();
                string? name = reader.GetString();
                if (name == null) throw new JsonException("Name field cannot be empty in trigger object.");

                return ITrigger.GetTrigger(name);
            }

            return null;
        }
    }
}
