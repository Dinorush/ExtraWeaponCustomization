using EWC.CustomWeapon.Properties.Effects.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public sealed class TriggerCoordinatorConverter : JsonConverter<TriggerCoordinator>
    {
        public override TriggerCoordinator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            TriggerCoordinator coordinator = new();

            // Simple trigger case (just one activation trigger)
            if (reader.TokenType == JsonTokenType.String)
            {
                ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                if (trigger != null)
                    coordinator.Activate.Add(trigger);
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
                coordinator.Activate.Add(customTrigger);
                return coordinator;
            }

            // Full object case
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return coordinator.Activate.Count > 0 ? coordinator : null;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");

                string property = reader.GetString()!;
                reader.Read();
                switch (property.ToLowerInvariant().Replace(" ", ""))
                {
                    case "cap":
                        coordinator.Cap = reader.GetUInt32();
                        break;
                    case "cooldown":
                        coordinator.Cooldown = reader.GetSingle();
                        break;
                    case "chance":
                        coordinator.Chance = reader.GetSingle();
                        break;
                    case "cooldownonapply":
                        coordinator.CooldownOnApply = reader.GetSingle();
                        break;
                    case "activate":
                    case "triggers":
                    case "trigger":
                        coordinator.Activate = DeserializeTriggers(ref reader, options);
                        if (coordinator.Activate?.Any() != true) return null;
                        break;
                    case "apply":
                        coordinator.Apply = DeserializeTriggers(ref reader, options);
                        break;
                    case "reset":
                        coordinator.Reset = DeserializeTriggers(ref reader, options);
                        break;
                }
            }

            throw new JsonException("Expected EndObject token");
        }

        private static List<ITrigger>? DeserializeTriggers(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.StartObject)
            {
                ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                if (trigger == null) return null;
                return new List<ITrigger>() { trigger };
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<ITrigger> result = new();
                reader.Read();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                    reader.Read();
                    if (trigger == null) return null;
                    result.Add(trigger);
                }
                if (!result.Any()) return null;
                return result;
            }

            throw new JsonException("Expected trigger or list of triggers when deserializing triggers for Coordinator");
        }

        // Only called for templates, so don't need logic for customized coordinators/triggers
        public override void Write(Utf8JsonWriter writer, TriggerCoordinator? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.Activate[0].Name.ToString() ?? "Invalid");
        }
    }
}
