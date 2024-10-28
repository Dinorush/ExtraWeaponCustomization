﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties
{
    public abstract class WeaponPropertyBase : IWeaponProperty
    {
#pragma warning disable CS8618 // Set when registered to a CWC
        public CustomWeaponComponent CWC { get; set; }
#pragma warning restore CS8618

        private static readonly Dictionary<Type, List<PropertyInfo>> _classProperties = new();

        public bool ValidProperty()
        {
            return (CWC.IsGun && this is IGunProperty)
                || (CWC.IsMelee && this is IMeleeProperty);
        }

        public virtual WeaponPropertyBase Clone()
        {
            Type type = GetType();
            if (!_classProperties.ContainsKey(type))
            {
                List<PropertyInfo> properties = new();
                _classProperties.Add(type, properties);

                for (Type currentType = type; currentType.BaseType != null; currentType = currentType.BaseType)
                {
                    foreach (var prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        MethodInfo? mget = prop.GetGetMethod(false);
                        MethodInfo? mset = prop.GetSetMethod(true);

                        // Only want properties with public get and private set
                        if (mget == null || mset == null || mset.IsPublic || !prop.CanWrite) continue;

                        properties.Add(prop);
                    }
                }
            }

            WeaponPropertyBase copy = (WeaponPropertyBase)Activator.CreateInstance(type)!;
            foreach (var prop in _classProperties[type])
                prop.SetValue(copy, prop.GetValue(this));

            return copy;
        }

        public abstract void Serialize(Utf8JsonWriter writer);
        public abstract void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }
}