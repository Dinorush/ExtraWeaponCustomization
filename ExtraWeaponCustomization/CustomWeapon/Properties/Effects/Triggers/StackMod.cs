using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Structs;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class StackMod
    {
        public float Value
        {
            get
            {
                return Math.Max(_min, BaseValue * _stackValue.Value);
            }
        }

        public StackValue StackValue => _stackValue;
        
        protected StackValue _stackValue = new();
        public float BaseValue { get; private set; }
        private float _min;

        public StackMod(float value, float min = 0) => Reset(value, min);

        public StackMod(StackMod mod)
        {
            Copy(mod);
        }

        public virtual void Copy(StackMod mod)
        {
            BaseValue = mod.BaseValue;
            _min = mod._min;
            _stackValue = mod._stackValue;
        }

        public void Reset(float value, float min = 0)
        {
            BaseValue = value;
            _min = min;
            _stackValue.Reset();
        }

        public void SetMin(float min)
        {
            _min = min;
        }

        public void AddMod(float mod, StackType type)
        {
            _stackValue.Add(mod, type);
        }
    }
}
