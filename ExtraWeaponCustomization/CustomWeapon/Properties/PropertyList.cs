using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties
{
    public sealed class PropertyList
    {
        public readonly List<WeaponPropertyBase> Properties;
        public readonly Dictionary<Type, Trait>? Traits;
        public readonly List<IWeaponProperty<WeaponSetupContext>>? SetupCallbacks;
        public readonly List<IWeaponProperty<WeaponClearContext>>? ClearCallbacks;
        public readonly List<ReferenceProperty>? ReferenceProperties;

        public TempProperties? Owner { get; set; }

        public bool Override { get; set; } = false;
        public bool Empty => Properties.Count == 0;

        public PropertyList() => Properties = new(0);

        public PropertyList(List<WeaponPropertyBase> properties)
        {
            Properties = properties;

            // Deserialized objects don't need this since Clone() runs when putting them
            // into the properties attached to CWCs
            foreach (var baseProperty in Properties)
            {
                if (baseProperty is Trait trait)
                {
                    Traits ??= new();
                    if (!Traits.TryAdd(baseProperty.GetType(), trait)) continue;
                }

                if (baseProperty is IWeaponProperty<WeaponSetupContext> weaponSetupContext)
                {
                    SetupCallbacks ??= new();
                    SetupCallbacks.Add(weaponSetupContext);
                }

                if (baseProperty is IWeaponProperty<WeaponClearContext> weaponClearContext)
                {
                    ClearCallbacks ??= new();
                    ClearCallbacks.Add(weaponClearContext);
                }

                if (baseProperty is ReferenceProperty refProp)
                {
                    ReferenceProperties ??= new();
                    ReferenceProperties.Add(refProp);
                }
            }
        }

        public PropertyList Clone()
        {
            return new PropertyList(Properties.ConvertAll(property => property.Clone()));
        }
    }
}
