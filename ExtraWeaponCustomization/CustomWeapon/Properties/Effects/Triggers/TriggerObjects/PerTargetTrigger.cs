using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.JSON;
using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class PerTargetTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.PerTarget;
        public ITrigger Activate { get; private set; }
        public ITrigger Apply { get; private set; }
        public float Amount { get; private set; } = 1f;
        public float Cap { get; private set; } = 0f;

        public PerTargetTrigger()
        {
            Activate = null!;
            Apply = null!;
        }

        public PerTargetTrigger(ITrigger activate, ITrigger apply)
        {
            Activate = activate;
            Apply = apply;
        }

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;
        private readonly static TriggerName[] ValidActivates = new[] { TriggerName.PreHit, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge };
        private readonly static TriggerName[] ValidApplies = new[] { TriggerName.PreHit, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge, TriggerName.Kill };

        private readonly Dictionary<BaseDamageableWrapper, float> _targetAmounts = new();

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (Activate.Invoke(context, out var triggerAmt))
            {
                var damageable = ((WeaponHitDamageableContextBase)context).Damageable;
                if (!_targetAmounts.TryGetValue(TempWrapper.Set(damageable), out var stored))
                {
                    _targetAmounts.Keys
                        .Where(wrapper => wrapper.Object == null)
                        .ToList()
                        .ForEach(wrapper => _targetAmounts.Remove(wrapper));
                    _targetAmounts.Add(new(TempWrapper), stored = 0);
                }

                stored += triggerAmt * Amount;
                if (Cap > 0)
                    stored = Math.Min(stored, Cap);
                _targetAmounts[TempWrapper] = stored;
            }

            if (Apply.Invoke(context, out _))
            {
                var damageable = ((WeaponHitDamageableContextBase)context).Damageable;
                if (_targetAmounts.Remove(TempWrapper.Set(damageable), out amount))
                    return true;
            }
            
            return false;
        }

        public void Reset()
        {
            _targetAmounts.Clear();
            Activate.Reset();
            Apply.Reset();
        }

        public ITrigger Clone()
        {
            var copy = CopyUtil<PerTargetTrigger>.Clone(this);
            copy.Activate = Activate.Clone();
            copy.Apply = Apply.Clone();
            return copy;
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "store":
                case "activate":
                    Activate = EWCJson.Deserialize<ITrigger>(ref reader)!;
                    if (Activate == null || !ValidActivates.Contains(Activate.Name))
                        throw new JsonException($"PerTarget Trigger requires a trigger of type [{string.Join(", ", ValidActivates)}]!");
                    break;
                case "apply":
                    Apply = EWCJson.Deserialize<ITrigger>(ref reader)!;
                    if (Apply == null || !ValidApplies.Contains(Apply.Name))
                        throw new JsonException($"PerTarget Trigger requires a trigger of type [{string.Join(", ", ValidApplies)}]!");
                    break;
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
            }
        }
    }
}
