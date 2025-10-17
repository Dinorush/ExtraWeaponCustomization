using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties
{
    public abstract class WeaponPropertyBase : IWeaponProperty
    {
        private CustomWeaponComponent _cwc = null!;
        public CustomWeaponComponent CWC // Set when added to CWC
        {
            get => _cwc;
            set
            { 
                _cwc = value;
                CGC = _cwc.Weapon.IsType(WeaponType.Gun) ? _cwc.Cast<CustomGunComponent>() : null!;
                OnCWCSet(); 
            }
        }
        public CustomGunComponent CGC { get; private set; } = null!;

        public uint ID { get; private set; } = 0;
        public PropertyRef Reference { get; protected set; }
        private readonly static Dictionary<string, uint> s_stringToIDDict = new();
        private static uint s_nextID = uint.MaxValue;

        public WeaponPropertyBase() => Reference = new(this);

        public virtual bool ShouldRegister(Type contextType) => true;

        public virtual bool ValidProperty() =>
            CWC.Owner.IsType(RequiredOwnerType) &&
            CWC.Owner.IsAnyType(ValidOwnerType) &&
            CWC.Weapon.IsType(RequiredWeaponType) &&
            CWC.Weapon.IsAnyType(ValidWeaponType);

        protected virtual OwnerType RequiredOwnerType => OwnerType.Any;
        protected virtual OwnerType ValidOwnerType => OwnerType.Any;
        protected virtual WeaponType RequiredWeaponType => WeaponType.Any;
        protected virtual WeaponType ValidWeaponType => WeaponType.Any;

        public string GetValidTypes() => $"Required Owner Types [{RequiredOwnerType}], Valid Owner Types [{ValidOwnerType}], Required Weapon Types [{RequiredWeaponType}], Valid Weapon Types [{ValidWeaponType}]";

        public virtual WeaponPropertyBase Clone()
        {
            var copy = CopyUtil<WeaponPropertyBase>.Clone(this);
            copy.Reference = new(copy);
            return copy;
        }

        protected virtual void OnCWCSet() { }
        public virtual void OnReferenceSet()
        {
            if (this is IReferenceHolder refHolder)
            {
                foreach (var property in refHolder.Properties.ReferenceProperties.OrEmptyIfNull())
                {
                    if (CWC.TryGetReference(property.ReferenceID, out var prop))
                        refHolder.OnReferenceSet(prop);
                }
            }

            if (this is ITriggerCallback callback)
                callback.Trigger?.OnReferenceSet();
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

        public static uint StringIDToInt(string id)
        {
            if (!s_stringToIDDict.ContainsKey(id))
                s_stringToIDDict.Add(id, s_nextID--);

            return s_stringToIDDict[id];
        }
    }
}
