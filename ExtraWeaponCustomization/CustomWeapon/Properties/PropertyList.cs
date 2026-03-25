using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties
{
    public sealed class PropertyList
    {
        private static readonly List<WeaponPropertyBase> s_emptyList = new(0);

        public readonly List<WeaponPropertyBase> Values;

        public int Count => Values.Count;
        public bool Empty => Values.Count == 0;

        private bool _setup = false;

        public PropertyList() => Values = s_emptyList;

        public PropertyList(List<WeaponPropertyBase> properties)
        {
            Values = properties;
        }

        public WeaponPropertyBase this[int index] => Values[index];
        public IEnumerator<WeaponPropertyBase> GetEnumerator() => Values.GetEnumerator();

        public void SetCWC(CustomWeaponComponent cwc)
        {
            if (_setup)
            {
                EWCLogger.Error($"Property list {string.Join(", ", Values)} was setup twice! This shouldn't happen!");
                return;
            }
            _setup = true;

            // Deserialized objects don't need this since they aren't running on a CWC
            foreach (var baseProperty in Values)
            {
                baseProperty.CWC = cwc;
            }
        }

        public PropertyList Clone()
        {
            return new PropertyList(Values.ConvertAll(property => property.Clone()));
        }
    }
}
