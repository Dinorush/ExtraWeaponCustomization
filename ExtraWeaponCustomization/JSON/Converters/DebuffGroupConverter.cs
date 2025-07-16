using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public sealed class DebuffGroupConverter : JsonConverter<DebuffGroup>
    {
        public override DebuffGroup? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            DebuffGroup result = new();
            if (reader.TokenType == JsonTokenType.Null)
            {
                result.IDs = DebuffManager.DefaultGroupSet;
                return result;
            }

            result.IDs = new();
            // Simple trigger case (just one)
            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.Number)
            {
                result.IDs.Add(ReadToken(ref reader));
                return result;
            }

            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected DebuffIDs to be string, int, or array");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return result;

                result.IDs.Add(ReadToken(ref reader));
            }

            throw new JsonException("Expected EndArray token");
        }

        // Only called for templates, so don't need custom logic
        public override void Write(Utf8JsonWriter writer, DebuffGroup? value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(0);
        }

        private static uint ReadToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
                return DebuffManager.StringIDToInt(reader.GetString()!);

            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetUInt32();

            throw new JsonException("DebuffIDs expected a string or int but got " + reader.TokenType);
        }
    }
}
