using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageableTrigger<TContext> : DamageTypeTrigger<TContext> where TContext : WeaponHitDamageableContextBase
    {
        public float UniqueThreshold { get; private set; } = 0f;

        private Dictionary<BaseDamageableWrapper, float>? _uniqueCounts;
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public DamageableTrigger(TriggerName name, DamageType type = DamageType.Any) :
            base(name, type) {}

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (!base.Invoke(context, out amount)) return false;

            TContext tContext = (TContext) context;
            amount = InvokeInternal(tContext);
            if (amount == 0f) return false;

            if (_uniqueCounts != null)
            {
                TempWrapper.SetObject(tContext.Damageable);
                if (!_uniqueCounts.ContainsKey(TempWrapper))
                {
                    _uniqueCounts.Keys
                        .Where(wrapper => !wrapper.Alive)
                        .ToList()
                        .ForEach(wrapper => _uniqueCounts.Remove(wrapper));
                    _uniqueCounts[new BaseDamageableWrapper(TempWrapper)] = 0;
                }
                _uniqueCounts[TempWrapper] += amount;

                if (_uniqueCounts[TempWrapper] < UniqueThreshold)
                {
                    amount = 0f;
                    return true;
                }
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

        protected override bool CloneObject => UniqueThreshold > 0;

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
                case "uniquethreshold":
                    UniqueThreshold = reader.GetSingle();
                    if (UniqueThreshold > 0)
                        _uniqueCounts = new();
                    break;
            }
        }
    }
}
