﻿using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties
{
    public abstract class WeaponPropertyBase : IWeaponProperty
    {
#pragma warning disable CS8618 // Set when registered to a CWC
        public CustomWeaponComponent CWC { get; set; }
#pragma warning restore CS8618

        public uint ID { get; private set; } = 0;
        private readonly static Dictionary<string, uint> s_stringToIDDict = new();
        private static uint s_nextID = uint.MaxValue;

        public virtual bool ShouldRegister(Type contextType) => true;

        public virtual bool ValidProperty()
        {
            return (CWC.IsGun && this is IGunProperty)
                || (CWC.IsMelee && this is IMeleeProperty);
        }

        public virtual WeaponPropertyBase Clone()
        {
            return CopyUtil<WeaponPropertyBase>.Clone(this);
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
