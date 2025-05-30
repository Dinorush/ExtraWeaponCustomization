﻿using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using GTFO.API.JSON.Converters;
using EWC.JSON.Converters;
using EWC.Dependencies;

namespace EWC.JSON
{
    public static class EWCJson
    {
        private static readonly JsonSerializerOptions _setting = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
        };

        static EWCJson()
        {
            _setting.Converters.Add(new JsonStringEnumConverter());
            _setting.Converters.Add(new TriggerConverter());
            _setting.Converters.Add(new TriggerCoordinatorConverter());
            _setting.Converters.Add(new WeaponPropertyConverter());
            _setting.Converters.Add(new PropertyListConverter());
            _setting.Converters.Add(new ColorConverter());
            if (PDAPIWrapper.HasPData)
                _setting.Converters.Add(PDAPIWrapper.PersistentIDConverter!);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static T? Deserialize<T>(ref Utf8JsonReader reader)
        {
            return JsonSerializer.Deserialize<T>(ref reader, _setting);
        }

        public static object? Deserialize(Type type, string json)
        {
            return JsonSerializer.Deserialize(json, type, _setting);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }

        public static void Serialize<T>(Utf8JsonWriter writer, T value)
        {
            JsonSerializer.Serialize(writer, value, _setting);
        }

        public static void Serialize<T>(Utf8JsonWriter writer, string name, T value)
        {
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, value, _setting);
        }
    }
}
