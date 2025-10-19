using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageableTrigger<TContext> : DamageTypeTrigger<TContext> where TContext : WeaponHitDamageableContextBase
    {
        public float MinBackstabRequired { get; set; } = 0f;
        public float MaxBackstabRequired { get; set; } = 1f;
        public float UniqueThreshold { get; private set; } = 0f;
        public float UniqueCap { get; private set; } = 0f;
        public bool UniqueConsumeOnTrigger { get; private set; } = true;

        private Dictionary<BaseDamageableWrapper, float>? _uniqueCounts;
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public DamageableTrigger(TriggerName name, DamageType[] types) : base(name, types)
        {
            BlacklistType |= DamageType.Dead;
        }

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (!base.Invoke(context, out amount)) return false;

            TContext tContext = (TContext) context;
            float backstabFrac = tContext.OrigBackstab - 1;
            if (backstabFrac < MinBackstabRequired || backstabFrac > MaxBackstabRequired)
            {
                amount = 0;
                return false;
            }

            amount = InvokeInternal(tContext);
            if (amount == 0f) return false;

            if (_uniqueCounts != null)
            {
                TempWrapper.Set(tContext.Damageable);
                if (!_uniqueCounts.ContainsKey(TempWrapper))
                {
                    _uniqueCounts.Keys
                        .Where(wrapper => !wrapper.Alive)
                        .ToList()
                        .ForEach(wrapper => _uniqueCounts.Remove(wrapper));
                    _uniqueCounts[new BaseDamageableWrapper(TempWrapper)] = 0;
                }
                _uniqueCounts[TempWrapper] += amount;

                if (_uniqueCounts[TempWrapper] < UniqueThreshold || (UniqueCap > 0f && _uniqueCounts[TempWrapper] > UniqueCap))
                {
                    amount = 0f;
                    return true;
                }
                else if(UniqueConsumeOnTrigger)
                    _uniqueCounts[TempWrapper] -= UniqueThreshold;
            }

            return true;
        }

        protected virtual float InvokeInternal(TContext context) => Amount;

        public override void Reset()
        {
            base.Reset();
            _uniqueCounts?.Clear();
        }

        protected override bool CloneObject => _uniqueCounts != null;

        public override ITrigger Clone()
        {
            if (!CloneObject) return this;

            var trigger = (DamageableTrigger<TContext>) base.Clone();
            trigger._uniqueCounts = _uniqueCounts != null ? new() : null;
            return trigger;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "minbackstabrequired":
                case "minbackstab":
                    MinBackstabRequired = reader.GetSingle();
                    break;
                case "maxbackstabrequired":
                case "maxbackstab":
                    MaxBackstabRequired = reader.GetSingle();
                    break;
                case "uniquethreshold":
                    UniqueThreshold = reader.GetSingle();
                    break;
                case "uniquecap":
                    UniqueCap = reader.GetSingle();
                    break;
                case "uniqueconsumeontrigger":
                case "uniquedecreaseontrigger":
                    UniqueConsumeOnTrigger = reader.GetBoolean();
                    break;
            }

            if (_uniqueCounts == null && (UniqueThreshold > 0 || UniqueCap > 0))
                _uniqueCounts = new();
        }
    }
}
