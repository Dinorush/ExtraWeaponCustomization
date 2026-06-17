using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public sealed class PerTargetTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.PerTarget;
        public List<ITrigger> Activate { get; private set; }
        public List<ITrigger> Apply { get; private set; }
        public List<ITrigger>? Cancel { get; private set; }
        public float Amount { get; private set; } = 1f;
        public float Cap { get; private set; } = 0f;
        public float Threshold { get; private set; } = 0f;
        public bool ActivateScaleWithFalloff { get; private set; } = false;
        public bool ApplyAboveThreshold { get; private set; } = false;
        public bool OverrideAmount { get; private set; } = false;

        public PerTargetTrigger()
        {
            Activate = null!;
            Apply = null!;
        }

        public PerTargetTrigger(ITrigger activate, ITrigger apply)
        {
            Activate = new() { activate };
            Apply = new() { apply };
        }

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;
        private readonly static TriggerName[] ValidActivates = ITrigger.HitTriggers.Remove(TriggerName.Empty);
        private readonly static TriggerName[] ValidApplies = ValidActivates.Extend(TriggerName.Kill);

        private readonly Dictionary<BaseDamageableWrapper, float> _targetAmounts = new();

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is not WeaponHitDamageableContextBase baseContext) return false;

            foreach (var trigger in Activate)
            {
                if (trigger.Invoke(context, out var triggerAmt) && triggerAmt > 0)
                {
                    var damageable = ((WeaponHitDamageableContextBase)context).Damageable;
                    if (!_targetAmounts.TryGetValue(TempWrapper.Set(damageable), out var stored))
                    {
                        _targetAmounts.Keys
                            .Where(wrapper => wrapper.IsNull)
                            .ToList()
                            .ForEach(wrapper => _targetAmounts.Remove(wrapper));
                        _targetAmounts.Add(new(TempWrapper), stored = 0);
                    }

                    var addAmount = triggerAmt * Amount;
                    if (ActivateScaleWithFalloff)
                        addAmount *= baseContext.Falloff;

                    stored += triggerAmt * Amount;
                    if (Cap > 0)
                        stored = Math.Min(stored, Cap);
                    _targetAmounts[TempWrapper] = stored;
                }
            }

            foreach (var trigger in Apply)
            {
                if (trigger.Invoke(context, out var triggerAmt) && triggerAmt > 0)
                {
                    var damageable = ((WeaponHitDamageableContextBase)context).Damageable;
                    if (_targetAmounts.TryGetValue(TempWrapper.Set(damageable), out var stored) && stored >= Threshold)
                    {
                        if (OverrideAmount)
                            amount = Amount;
                        else if (ApplyAboveThreshold)
                            amount = stored - Threshold;
                        else
                            amount = stored;

                        if (amount == 0) return true;

                        _targetAmounts.Remove(TempWrapper);
                        return true;
                    }
                }
            }

            foreach (var trigger in Cancel.OrEmptyIfNull())
            {
                if (trigger.Invoke(context, out var triggerAmt) && triggerAmt > 0)
                {
                    var damageable = ((WeaponHitDamageableContextBase)context).Damageable;
                    _targetAmounts.Remove(TempWrapper.Set(damageable));
                }
            }
            
            return _targetAmounts.Count > 0;
        }

        public void Reset()
        {
            _targetAmounts.Clear();
            foreach (var trigger in Activate)
                trigger.Reset();
            foreach (var trigger in Apply)
                trigger.Reset();
            foreach (var trigger in Cancel.OrEmptyIfNull())
                trigger.Reset();
        }

        public ITrigger Clone()
        {
            var copy = CopyUtil.Clone(this);
            copy.Activate = Activate.ConvertAll(trigger => trigger.Clone());
            copy.Apply = Apply.ConvertAll(trigger => trigger.Clone());
            copy.Cancel = Cancel?.ConvertAll(trigger => trigger.Clone());
            return copy;
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "store":
                case "activate":
                    Activate = ITrigger.DeserializeList(ref reader)!;
                    if (!IsValidList(Activate, ValidActivates))
                        throw new JsonException($"PerTarget Trigger requires an activate trigger of type [{string.Join(", ", ValidActivates)}]!");
                    break;
                case "apply":
                    Apply = ITrigger.DeserializeList(ref reader)!;
                    if (!IsValidList(Apply, ValidApplies))
                        throw new JsonException($"PerTarget Trigger requires an apply trigger of type [{string.Join(", ", ValidApplies)}]!");
                    break;
                case "cancel":
                case "reset":
                    Cancel = ITrigger.DeserializeList(ref reader)!;
                    if (Cancel == null) break;
                    if (!IsValidList(Cancel, ValidActivates))
                        throw new JsonException($"PerTarget Trigger requires a cancel trigger of type [{string.Join(", ", ValidActivates)}]!");
                    break;
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "threshold":
                    Threshold = reader.GetSingle();
                    break;
                case "applyabovethreshold":
                    ApplyAboveThreshold = reader.GetBoolean();
                    break;
                case "activatescalewithfalloff":
                case "scalewithfalloff":
                    ActivateScaleWithFalloff = reader.GetBoolean();
                    break;
                case "overrideamount":
                case "applyoverrideamount":
                case "applyamount":
                    OverrideAmount = reader.GetBoolean();
                    return;
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
            }
        }

        private static bool IsValidList(List<ITrigger>? list, TriggerName[] validList)
        {
            if (list == null) return false;

            foreach (ITrigger trigger in list)
                if (!validList.Contains(trigger.Name))
                    return false;
            return true;
        }
    }
}
