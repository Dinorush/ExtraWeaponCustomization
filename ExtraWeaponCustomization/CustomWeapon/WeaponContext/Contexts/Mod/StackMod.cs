using EWC.CustomWeapon.Enums;
using System;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class StackMod
    {
        public float Value
        {
            get
            {
                return Math.Max(_min, _value * (_overrideMod >= 0 ? _overrideMod : _addMod * _multMod * _maxMod));
            }
        }

        private readonly float _value;
        private float _min;
        private float _addMod = 1f;
        private float _multMod = 1f;
        private float _maxMod = 1f;
        private float _overrideMod = -1f;

        public StackMod(float value, float min = 0)
        {
            _value = value;
            _min = min;
        }

        public void SetMin(float min)
        {
            _min = min;
        }

        public void AddMod(float mod, StackType type)
        {
            switch (type)
            {
                case StackType.Override:
                    _overrideMod = mod;
                    break;
                case StackType.Add:
                    _addMod += (mod - 1);
                    break;
                case StackType.Multiply:
                    _multMod *= mod;
                    break;
                case StackType.Max:
                    if (mod > 1f)
                        _maxMod = Math.Max(_maxMod, mod);
                    else if (mod < 1f)
                        _maxMod = Math.Min(_maxMod, mod);
                    break;
            }
        }
    }
}
