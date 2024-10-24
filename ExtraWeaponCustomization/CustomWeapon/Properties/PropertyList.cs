using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Traits;
using System;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties
{
    public sealed class PropertyList
    {
        public readonly List<WeaponPropertyBase> Properties;
        public readonly Dictionary<Type, Trait>? Traits;
        public TempProperties? Owner { get; set; }

        public bool Override { get; set; } = false;

        public PropertyList(List<WeaponPropertyBase> properties, bool cacheTypes = true)
        {
            Properties = properties;
            if (!cacheTypes) return;

            // Deserialized objects don't need this since Clone() runs when putting them
            // into the properties attached to CWCs
            foreach (var baseProperty in Properties)
            {
                if (baseProperty is Trait trait)
                {
                    Traits ??= new();
                    Traits.TryAdd(baseProperty.GetType(), trait);
                }
            }
        }

        public PropertyList Clone()
        {
            return new PropertyList(Properties.ConvertAll(property => property.Clone()));
        }
    }
}
