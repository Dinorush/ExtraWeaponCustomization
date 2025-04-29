using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ActivateHolder : TriggerHolder
    {
        public float ApplyDelay { get; private set; } = 0f;
        public float CooldownOnApply { get; private set; } = 0f;
        public uint CooldownOnApplyThreshold { get; private set; } = 1;
        public bool ApplyAboveThreshold { get; private set; } = false;
        public float ApplyOverrideAmount { get; private set; } = 0f;

        private readonly List<TriggerContext> _accumulatedTriggers = new();
        private uint _applyCount = 0;
        private Queue<DelayedCallback>? _delayedApplies;

        public ActivateHolder(TriggerCoordinator parent, params ITrigger[] triggers) : base(parent, triggers) { }

        public override TriggerHolder Clone(TriggerCoordinator parent)
        {
            var copy = (ActivateHolder) base.Clone(parent);
            copy._delayedApplies = _delayedApplies != null ? new() : null;
            return copy;
        }

        public void ApplyTriggers()
        {
            // Need to copy list and reset prior to applying trigger in case applying triggers causes another application
            if (ApplyAboveThreshold)
            {
                int i = 0;
                for (float sum = 0; (sum += _accumulatedTriggers[i].triggerAmt) < Threshold; i++) ;

                _accumulatedTriggers.RemoveRange(0, i);
            }

            List<TriggerContext> currentTriggers;
            if (ApplyOverrideAmount > 0f)
            {
                currentTriggers = new((int)ApplyOverrideAmount);
                float total = 0f;
                // Iterates backwards so most recent triggers are prioritized for more reactive behavior.
                // Only last trigger is adjusted to account for underflow or overflow since it's the simplest solution.
                for (int i = _accumulatedTriggers.Count - 1; i >= 0 && total < ApplyOverrideAmount; i--)
                {
                    total += _accumulatedTriggers[i].triggerAmt;
                    currentTriggers.Add(_accumulatedTriggers[i]);
                }

                if (total != ApplyOverrideAmount)
                {
                    var tContext = currentTriggers[^1];
                    tContext.triggerAmt += ApplyOverrideAmount - total;
                    currentTriggers[^1] = tContext;
                }
            }
            else
                currentTriggers = new(_accumulatedTriggers);

            _triggerSum = 0f;
            _accumulatedTriggers.Clear();

            if (++_applyCount >= CooldownOnApplyThreshold)
            {
                SetCooldown(CooldownOnApply);
                _applyCount = 0;
            }

            DoApply(currentTriggers);
        }

        private void DoApply(List<TriggerContext> triggerContexts)
        {
            if (ApplyDelay > 0f)
            {
                var callback = new DelayedCallback(ApplyDelay, () =>
                {
                    Caller?.TriggerApply(triggerContexts);
                    _delayedApplies!.Dequeue();
                });
                _delayedApplies!.Enqueue(callback);
                StartDelayedCallback(callback);
            }
            else
                Caller?.TriggerApply(triggerContexts);
        }

        public override void Reset(bool resetAccumulated = true)
        {
            if (resetAccumulated)
            {
                _accumulatedTriggers.Clear();
                _applyCount = 0;
            }

            if (ApplyDelay > 0)
            {
                while (_delayedApplies!.TryDequeue(out var callback))
                    callback.Cancel();
            }

            base.Reset(resetAccumulated);
        }

        protected override void DelayedReset()
        {
            _accumulatedTriggers.Clear();
            base.DelayedReset();
        }

        protected override void OnAddTrigger(WeaponTriggerContext context, float amount)
        {
            _accumulatedTriggers.Add(new TriggerContext { triggerAmt = amount, context = context });
        }

        protected override void OnCancel()
        {
            if (CancelReduceAmount > 0f && _accumulatedTriggers.Count > 0)
            {
                int i = _accumulatedTriggers.Count - 1;
                for (float sum = 0; i > 0 && (sum += _accumulatedTriggers[i].triggerAmt) < CancelReduceAmount; i--) ;

                _accumulatedTriggers.RemoveRange(i, _accumulatedTriggers.Count - i - 1);
            }
            else
                _accumulatedTriggers.Clear();
            base.OnCancel();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "applydelay":
                case "delay":
                    ApplyDelay = reader.GetSingle();
                    if (ApplyDelay > 0f)
                        _delayedApplies = new();
                    break;
                case "cooldownonapply":
                    CooldownOnApply = reader.GetSingle();
                    return;
                case "cooldownonapplythreshold":
                    CooldownOnApplyThreshold = Math.Max(reader.GetUInt32(), 1);
                    return;
                case "applyabovethreshold":
                    ApplyAboveThreshold = reader.GetBoolean();
                    return;
                case "applyoverrideamount":
                case "applyamount":
                    ApplyOverrideAmount = reader.GetSingle();
                    return;
            }
        }
    }
}
