using ExtraWeaponCustomization.CustomWeapon.Properties;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExtraWeaponCustomization.JSON.Converters
{
    public sealed class PropertyListConverter : JsonConverter<PropertyList>
    {
        public override PropertyList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            List<IWeaponProperty>? list = EWCJson.Deserialize<List<IWeaponProperty>>(ref reader);
            if (list == null || list.Count == 0) return null;
            return new PropertyList(list, false);
        }

        public override void Write(Utf8JsonWriter writer, PropertyList? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            EWCJson.Serialize(value.Properties);
        }
    }
}
