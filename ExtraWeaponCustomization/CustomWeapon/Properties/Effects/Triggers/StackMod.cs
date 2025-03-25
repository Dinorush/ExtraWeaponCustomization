using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class StackMod
    {
        public float Value
        {
            get
            {
                return Math.Max(_min, _value * (_override ? _overrideMod : _addMod * _multMod * _maxMod));
            }
        }

        private float _value;
        private float _min;
        protected float _addMod;
        protected float _multMod;
        protected float _maxMod;
        protected bool _override;
        protected float _overrideMod;

        public StackMod(float value, float min = 0) => Reset(value, min);

        public StackMod(StackMod mod)
        {
            Copy(mod);
        }

        public virtual void Copy(StackMod mod)
        {
            _value = mod._value;
            _min = mod._min;
            _addMod = mod._addMod;
            _multMod = mod._multMod;
            _maxMod = mod._maxMod;
            _overrideMod = mod._overrideMod;
            _override = mod._override;
        }

        public void Reset(float value, float min = 0)
        {
            _value = value;
            _min = min;
            _addMod = 1f;
            _multMod = 1f;
            _maxMod = 1f;
            _override = false;
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
                    _override = true;
                    break;
                case StackType.Add:
                    _addMod += mod - 1;
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

        public float Combine(DamageType damageType, IDamageable damageable, params ShotStackMod[] mods)
        {
            if (_override)
                return _overrideMod;

            float addMod = _addMod;
            float multMod = _multMod;
            float maxMod = _maxMod;

            foreach (var mod in mods)
            {
                if (!mod.Recompute(damageType, damageable)) continue;

                if (mod._override)
                    return mod._overrideMod;
                addMod += mod._addMod - 1f;
                multMod *= mod._multMod;
                maxMod = _maxMod > 1f || (_maxMod == 1f && mod._maxMod > 1f) ? Math.Max(_maxMod, mod._maxMod) : Math.Min(_maxMod, mod._maxMod);
            }

            return Math.Max(_min, _value * addMod * multMod * maxMod);
        }
    }
}
