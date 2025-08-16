using EWC.CustomWeapon.Properties.Effects.Triggers;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public sealed class TriggerCoordinatorConverter : JsonConverter<TriggerCoordinator>
    {
        public override TriggerCoordinator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            TriggerCoordinator coordinator = new();

            // Simple trigger case (just one activation trigger)
            if (reader.TokenType == JsonTokenType.String)
            {
                ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                if (trigger != null)
                    coordinator.Activate.Triggers.Add(trigger);
                else
                    return null;
                return coordinator;
            }

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected Trigger Coordinator to be either a string or object");

            // Customized trigger case
            ITrigger? customTrigger = null;
            try
            {
                customTrigger = EWCJson.Deserialize<ITrigger>(ref reader);
            }
            catch (JsonException) { }

            if (customTrigger != null)
            {
                coordinator.Activate.Triggers.Add(customTrigger);
                return coordinator;
            }

            // Full object case
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return coordinator;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");

                string property = reader.GetString()!;
                reader.Read();
                coordinator.DeserializeProperty(property.ToLowerInvariant().Replace(" ", ""), ref reader);
            }

            throw new JsonException("Expected EndObject token");
        }

        // Only called for templates, so don't need logic for customized coordinators/triggers
        public override void Write(Utf8JsonWriter writer, TriggerCoordinator? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Activate.Triggers[0].Name.ToString());
        }
    }
}
