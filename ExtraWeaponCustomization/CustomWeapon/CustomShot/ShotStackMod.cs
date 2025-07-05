using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using System;
using System.Collections.Generic;

namespace EWC.CustomWeapon.CustomShot
{
    public sealed class ShotStackMod : StackMod
    {
        private Dictionary<BaseDamageableWrapper, Dictionary<WeaponPropertyBase, (float, StackType, DamageType[])>>? _modsPerTarget;
        private Dictionary<BaseDamageableWrapper, Dictionary<WeaponPropertyBase, (float mod, StackType layer, DamageType[] types)>> ModsPerTarget => _modsPerTarget ??= new(3);
        private Dictionary<WeaponPropertyBase, (float, StackType, DamageType[])>? _mods;
        private Dictionary<WeaponPropertyBase, (float mod, StackType layer, DamageType[] types)> Mods => _mods ??= new(3);

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;
        private DamageType _lastType = DamageType.Invalid;
        private IntPtr _lastDamPtr = IntPtr.Zero;
        private bool _needType = false;
        private bool _needRecompute = false;

        public ShotStackMod(float min = 0) : base(1f, min)
        {
            _mods = null;
            _modsPerTarget = null;
        }

        public ShotStackMod(ShotStackMod mod) : base(mod)
        {
            _mods = mod._mods != null ? new(mod._mods) : null;
            if (mod._modsPerTarget != null)
            {
                _modsPerTarget = new(mod._modsPerTarget);
                foreach ((var wrapper, var dict) in mod._modsPerTarget)
                    _modsPerTarget[wrapper] = new(dict);
            }
            else
                _modsPerTarget = null;

            _needRecompute = mod._needRecompute;
            _needType = mod._needType;
            _lastDamPtr = mod._lastDamPtr;
            _lastType = mod._lastType;
        }

        public void AddMod(TriggerMod triggerMod, float mod, IDamageable? damageable = null, params DamageType[] types) => AddMod(triggerMod, mod, triggerMod.Cap, triggerMod.StackType, triggerMod.StackLayer, damageable, types);

        public void AddMod(WeaponPropertyBase property, float mod, float cap, StackType stack, StackType layer, IDamageable? damageable = null, params DamageType[] types)
        {
            if (types.Length == 0)
                types = DamageTypeConst.Any;

            Dictionary<WeaponPropertyBase, (float mod, StackType, DamageType[])> mods;
            if (damageable != null)
            {
                TempWrapper.Set(damageable);
                if (!ModsPerTarget.TryGetValue(TempWrapper, out var dict))
                    ModsPerTarget.Add(new BaseDamageableWrapper(TempWrapper), mods = new(3));
                else
                    mods = dict;
            }
            else
                mods = Mods;

            if (mods.TryGetValue(property, out var value))
            {
                float oldMod = value.mod;
                switch (stack)
                {
                    case StackType.Override:
                        value.mod = mod;
                        break;
                    case StackType.Add:
                        value.mod += mod - 1f;
                        break;
                    case StackType.Multiply:
                        value.mod *= mod;
                        break;
                    case StackType.Max:
                        if (mod > 1f)
                            value.mod = Math.Max(value.mod, mod);
                        else if (mod < 1f)
                            value.mod = Math.Min(value.mod, mod);
                        break;
                }
                value.mod = cap > 1f ? Math.Min(value.mod, cap) : Math.Max(value.mod, cap);
                if (oldMod == value.mod) return;
                mods[property] = value;
            }
            else
                mods[property] = (mod, layer, types);

            _needType = _needType || types.Length > 1 || types.Length == 1 && types[0] != DamageType.Any;
            _needRecompute = true;
        }

        public void Reset(float min = 0f) => Reset(1f, min);

        public bool Recompute(DamageType damageType = DamageType.Any, IDamageable? damageable = null)
        {
            if (_mods == null && _modsPerTarget == null) return false;

            IntPtr ptr = damageable?.Pointer ?? IntPtr.Zero;
            if (!_needRecompute && (!_needType || _lastType == damageType) && (_modsPerTarget == null || _lastDamPtr == ptr)) return true;

            _addMod = 1f;
            _multMod = 1f;
            _maxMod = 1f;
            _override = false;
            _lastDamPtr = ptr;
            _lastType = damageType;
            _needRecompute = false;

            if (_mods != null)
            {
                foreach (var (mod, layer, types) in Mods.Values)
                    if (damageType.HasFlagIn(types))
                        AddMod(mod, layer);
            }

            if (_modsPerTarget != null && ptr != IntPtr.Zero)
            {
                TempWrapper.Set(damageable!);
                if (ModsPerTarget.TryGetValue(TempWrapper, out var mods))
                    foreach (var (mod, layer, types) in mods.Values)
                        if (damageType.HasFlagIn(types))
                            AddMod(mod, layer);
            }

            return true;
        }
    }
}
