using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties
{
    public abstract class WeaponPropertyBase : IWeaponProperty
    {
        public CustomWeaponComponent CWC { get; set; } = null!; // Set when added to CWC

        public uint ID { get; private set; } = 0;
        public PropertyRef Reference { get; protected set; }
        private readonly static Dictionary<string, uint> s_stringToIDDict = new();
        private static uint s_nextID = uint.MaxValue;

        public WeaponPropertyBase() => Reference = new(this);

        public virtual bool ShouldRegister(Type contextType) => true;

        public virtual bool ValidProperty()
        {
            return (CWC.IsGun && this is IGunProperty)
                || (CWC.IsMelee && this is IMeleeProperty);
        }

        public virtual WeaponPropertyBase Clone()
        {
            var copy = CopyUtil<WeaponPropertyBase>.Clone(this);
            copy.Reference = new(copy);
            return copy;
        }

        public abstract void Serialize(Utf8JsonWriter writer);

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "id":
                    if (reader.TokenType == JsonTokenType.String)
                        ID = StringIDToInt(reader.GetString()!);
                    else
                        ID = reader.GetUInt32();
                    break;
            }
        }

        protected static uint StringIDToInt(string id)
        {
            if (!s_stringToIDDict.ContainsKey(id))
                s_stringToIDDict.Add(id, s_nextID--);

            return s_stringToIDDict[id];
        }
    }
}
