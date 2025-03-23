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
        public Dictionary<Type, Trait>? Traits { get; private set; }
        public List<IWeaponProperty<WeaponSetupContext>>? SetupCallbacks { get; private set; }
        public List<IWeaponProperty<WeaponClearContext>>? ClearCallbacks { get; private set; }
        public List<ReferenceProperty>? ReferenceProperties { get; private set; }
        public List<IReferenceHolder>? ReferenceHolders { get; private set; }

        public TempProperties? Owner { get; set; }

        public bool Override { get; set; } = false;
        public bool Empty => Properties.Count == 0;

        private bool _setup = false;

        public PropertyList() => Properties = new(0);

        public PropertyList(List<WeaponPropertyBase> properties)
        {
            Properties = properties;
        }

        public void SetCWC(CustomWeaponComponent cwc)
        {
            if (_setup)
            {
                Utils.Log.EWCLogger.Error($"Property list {string.Join(", ", Properties)} was setup twice! This shouldn't happen!");
                return;
            }
            _setup = true;

            // Deserialized objects don't need this since they aren't running on a CWC
            foreach (var baseProperty in Properties)
            {
                baseProperty.CWC = cwc;

                if (baseProperty is Trait trait)
                {
                    Traits ??= new();
                    if (!Traits.TryAdd(baseProperty.GetType(), trait)) continue;
                }

                if (baseProperty.IsProperty<WeaponSetupContext>(out var weaponSetupContext))
                {
                    SetupCallbacks ??= new();
                    SetupCallbacks.Add(weaponSetupContext);
                }

                if (baseProperty.IsProperty<WeaponClearContext>(out var weaponClearContext))
                {
                    ClearCallbacks ??= new();
                    ClearCallbacks.Add(weaponClearContext);
                }

                if (baseProperty is ReferenceProperty refProp)
                {
                    ReferenceProperties ??= new();
                    ReferenceProperties.Add(refProp);
                }

                if (baseProperty is IReferenceHolder refHolder)
                {
                    ReferenceHolders ??= new();
                    ReferenceHolders.Add(refHolder);
                }
            }
        }

        public PropertyList Clone()
        {
            return new PropertyList(Properties.ConvertAll(property => property.Clone()));
        }
    }

    public interface IReferenceHolder : IWeaponProperty
    {
        public PropertyList Properties { get; }
        public void OnReferenceSet(WeaponPropertyBase property);
    }
}
