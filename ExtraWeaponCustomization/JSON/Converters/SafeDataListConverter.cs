using EWC.CustomWeapon;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public class SafeDataListConverter : JsonConverter<List<CustomWeaponData>>
    {
        public override List<CustomWeaponData> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            List<CustomWeaponData> list = new();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                var data = SafeReadData(ref reader, options);
                if (data != null)
                    list.Add(data);
                return list;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return list;

                var data = SafeReadData(ref reader, options);
                if (data != null)
                    list.Add(data);
            }

            EWCLogger.Error($"Unable to find EndArray token for {typeof(CustomWeaponData).Name} list.");
            return list;
        }

        private static CustomWeaponData? SafeReadData(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            CustomWeaponData? obj;
            string issue = string.Empty;
            var backup = reader;
            try
            {
                obj = JsonSerializer.Deserialize<CustomWeaponData>(ref reader, options);
                if (obj == null)
                    issue = "Read as null";
            }
            catch (JsonException e)
            {
                obj = null;
                issue = e.Message;
            }

            if (obj == null)
            {
                (var name, var id) = GetInfoAndSkip(ref backup);
                EWCLogger.Error($"Error parsing {name} ({id}): {issue}");
                reader = backup;
            }
            return obj;
        }

        private static (string, uint) GetInfoAndSkip(ref Utf8JsonReader reader)
        {
            bool nameCheck = false;
            int idCheck = 0;
            string debugName = "No Name";
            uint id = 0;
            if (reader.TokenType != JsonTokenType.StartObject) return (debugName, id);

            int objCount = 1;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                    objCount++;

                if (reader.TokenType == JsonTokenType.EndObject && --objCount == 0)
                    break;

                if (nameCheck && idCheck == 2) continue;

                if (objCount != 1 || reader.TokenType != JsonTokenType.PropertyName) continue;

                string property = reader.GetString()!.ToLowerInvariant();
                if (!nameCheck && property == "name")
                {
                    reader.Read();
                    string? name = reader.GetString();
                    if (name != null)
                        debugName = name;
                    nameCheck = true;
                }
                else if (idCheck < 2)
                {
                    if (property == "archetypeid")
                    {
                        reader.Read();
                        id = reader.GetUInt32();
                        idCheck++;
                        if (id != 0)
                            idCheck = 2;
                    }
                    else if (property == "meleearchetypeid")
                    {
                        reader.Read();
                        id = reader.GetUInt32();
                        idCheck++;
                        if (id != 0)
                            idCheck = 2;
                    }
                }
            }

            return (debugName, id);
        }

        public override void Write(Utf8JsonWriter writer, List<CustomWeaponData> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var obj in value)
                JsonSerializer.Serialize(writer, obj, options);
            writer.WriteEndArray();
        }
    }
}
