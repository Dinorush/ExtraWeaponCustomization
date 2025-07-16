using EWC.CustomWeapon.Enums;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile
{
    public sealed class ProjectileStatChange
    {
        public float EndFrac { get; set; } = 1f;
        public StatType StatType { get; set; } = StatType.Damage;
        public float Delay { get; set; } = 0f;
        public float ChangeTime { get; set; } = 0f;

        public void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(EndFrac), EndFrac);
            writer.WriteString(nameof(StatType), StatType.ToString());
            writer.WriteNumber(nameof(Delay), Delay);
            writer.WriteNumber(nameof(ChangeTime), ChangeTime);
            writer.WriteEndObject();
        }

        public static void SerializeList(List<ProjectileStatChange> statChanges, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var statChange in statChanges)
                statChange.Serialize(writer);
            writer.WriteEndArray();
        }

        private void DeserializeProperty(string propertyName, ref Utf8JsonReader reader)
        {
            switch (propertyName)
            {
                case "endfrac":
                case "frac":
                case "mod":
                    EndFrac = reader.GetSingle();
                    break;
                case "stattype":
                case "stat":
                    StatType = reader.GetString().ToEnum(StatType.Damage);
                    break;
                case "delay":
                    Delay = reader.GetSingle();
                    break;
                case "changetime":
                case "time":
                case "duration":
                    ChangeTime = reader.GetSingle();
                    break;
            }
        }

        public void Deserialize(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new Exception("Expected StartObject token");

            while (reader.Read())
            {
                Utils.Log.EWCLogger.Log($"Read token {reader.TokenType}");
                if (reader.TokenType == JsonTokenType.EndObject) return;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");

                string property = reader.GetString()!;
                Utils.Log.EWCLogger.Log($"Read property {property}");
                reader.Read();
                Utils.Log.EWCLogger.Log($"Read token type {reader.TokenType}");
                DeserializeProperty(property.ToLowerInvariant().Replace(" ", ""), ref reader);
            }

            throw new JsonException("Expected EndObject token");
        }

        public static List<ProjectileStatChange> DeserializeList(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new Exception("Expected StartArray token");

            List<ProjectileStatChange> statChanges = new();
            while(reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return statChanges;

                ProjectileStatChange statChange = new();
                statChange.Deserialize(ref reader);
                statChanges.Add(statChange);
            }

            throw new JsonException("Expected EndArray token");
        }
    }
}
