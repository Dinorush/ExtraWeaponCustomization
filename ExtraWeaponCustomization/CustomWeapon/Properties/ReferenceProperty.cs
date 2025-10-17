using System.Text.Json;

namespace EWC.CustomWeapon.Properties
{
    public sealed class ReferenceProperty : 
        WeaponPropertyBase
    {
        public ReferenceProperty() { }

        public ReferenceProperty(uint id)
        {
            ReferenceID = id;
        }
        public ReferenceProperty(string id)
        {
            ReferenceID = StringIDToInt(id);
        }

        public uint ReferenceID { get; private set; } = 0;

        public override void OnReferenceSet()
        {
            base.OnReferenceSet();
            if (CWC.TryGetReferenceHolder(ReferenceID, out var propRef))
                Reference = propRef;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber("ID", ReferenceID);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "referenceid":
                case "id":
                    if (reader.TokenType == JsonTokenType.String)
                        ReferenceID = StringIDToInt(reader.GetString()!);
                    else
                        ReferenceID = reader.GetUInt32();
                    break;
            }
        }
    }
}
