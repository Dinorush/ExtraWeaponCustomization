using EWC.CustomWeapon.Properties;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EWC.JSON.Converters
{
    public sealed class PropertyListConverter : JsonConverter<PropertyList>
    {
        public override PropertyList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            List<WeaponPropertyBase>? list = EWCJson.Deserialize<List<WeaponPropertyBase>>(ref reader);
            if (list == null) throw new JsonException("Unable to deserialize property list");

            return new PropertyList(list);
        }

        public override void Write(Utf8JsonWriter writer, PropertyList value, JsonSerializerOptions options)
        {
            EWCJson.Serialize(writer, value.Properties);
        }
    }
}
