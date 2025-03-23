using System;
using System.Collections.Generic;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;
using EWC.Utils;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class TriggerCoordinator
    {
        public ITriggerCallback? Parent { get; set; }
        public TriggerHolder Activate { get; private set; }
        public bool ConsumeThresholdOnActivate { get; private set; } = false;

        public TriggerHolder? Apply { get; private set; }
        public float ApplyDelay { get; private set; } = 0f;
        public float CooldownOnApply { get; private set; } = 0f;
        public uint CooldownOnApplyThreshold { get; private set; } = 1;
        public bool ApplyAboveThreshold { get; private set; } = false;
        public float ApplyOverrideAmount { get; private set; } = 0f;

        public TriggerHolder? Reset { get; private set; }
        public float ResetDelay { get; private set; } = 0f;
        public bool ResetPreviousOnly { get; private set; } = false;

        public TriggerHolder? Cancel { get; private set; }
        public float CancelDelay { get; private set; } = 0f;

        private uint _applyCount = 0;
        private Queue<DelayedCallback>? _delayedApplies;
        private DelayedCallback? _delayedReset;
        private DelayedCallback? _delayedCancel;
        private readonly List<TriggerContext> _accumulatedTriggers = new();

        public TriggerCoordinator(params ITrigger[] triggers)
        {
            Activate = new(this, false, () =>_accumulatedTriggers.Clear(), triggers);
        }

        public TriggerCoordinator Clone()
        {
            var copy = CopyUtil<TriggerCoordinator>.Clone(this);
            copy.Activate = Activate.Clone(copy, () => copy._accumulatedTriggers.Clear());
            copy.Apply = Apply?.Clone(copy, () => copy._applyCount = 0);
            copy.Reset = Reset?.Clone(copy);
            copy._delayedApplies = _delayedApplies != null ? new() : null;
            copy._delayedReset = _delayedReset != null ? new(ResetDelay, copy.ForceReset) : null;
            copy._delayedCancel = _delayedCancel != null ? new(CancelDelay, copy.DoCancel) : null;
            return copy;
        }

        public void Invoke(WeaponTriggerContext context)
        {
            // Store valid activations (if any) and whether they should be applied.
            bool apply = false;
            if (Activate.Invoke(context, _accumulatedTriggers))
            {
                if (ConsumeThresholdOnActivate)
                    Activate.ReduceAccumulated();
                apply = Apply?.Invoke(context) ?? true;
            }

            // Check if we will want to reset.
            // Necessary to check before applying for ResetPreviousOnly to function correctly.
            bool reset = Reset?.Invoke(context) ?? false;
            bool cancel = Cancel?.Invoke(context) ?? false;

            // Similar to a standard reset, but fields needed to apply stored activations are preserved
            if (ResetPreviousOnly && reset)
                ResetTriggers(!apply);

            // Apply stored activations. If there are no Apply triggers, stored activations apply immediately
            if (apply)
                ApplyTriggers();

            // Reset stored activations AND any related behavior on the callback this coordinator is tied to
            if (!ResetPreviousOnly && reset)
                ResetTriggers();

            if (!reset && cancel)
                CancelTriggers();
        }

        private void ApplyTriggers()
        {
            // Need to copy list and reset prior to applying trigger in case applying triggers causes another application
            if (ApplyAboveThreshold)
            {
                int i = 0;
                for(float sum = 0; (sum += _accumulatedTriggers[i].triggerAmt) < Activate.Threshold; i++);

                _accumulatedTriggers.RemoveRange(0, i);
            }

            List<TriggerContext> currentTriggers;
            if (ApplyOverrideAmount > 0f)
            {
                currentTriggers = new((int) ApplyOverrideAmount);
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

            Activate.ClearAccumulated();
            Apply?.ClearAccumulated();
            _accumulatedTriggers.Clear();

            if (++_applyCount >= CooldownOnApplyThreshold)
            {
                Activate.SetCooldown(CooldownOnApply);
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
                    Parent?.TriggerApply(triggerContexts);
                    _delayedApplies!.Dequeue();
                });
                _delayedApplies!.Enqueue(callback);
                StartDelayedCallback(callback);
            }
            else
                Parent?.TriggerApply(triggerContexts);
        }

        private void ResetTriggers(bool resetAccumulated = true)
        {
            if (ResetDelay > 0f)
                StartDelayedCallback(_delayedReset!, checkEnd: true, refresh: false);
            else
                DoReset(resetAccumulated);
        }

        private void DoReset(bool resetAccumulated = true)
        {
            if (resetAccumulated)
            {
                _accumulatedTriggers.Clear();
                _applyCount = 0;
            }

            Parent?.TriggerReset();
            Activate.Reset();
            Apply?.Reset();
            Reset?.Reset();

            if (ApplyDelay > 0)
            {
                while (_delayedApplies!.TryDequeue(out var callback))
                    callback.Cancel();
            }
        }

        public void ForceReset() => DoReset(true);

        private void CancelTriggers()
        {
            if (CancelDelay > 0f)
                StartDelayedCallback(_delayedCancel!, checkEnd: true, refresh: false);
            else
                DoCancel();
        }

        private void DoCancel()
        {
            _accumulatedTriggers.Clear();
            _applyCount = 0;
            Activate.Reset();
            Apply?.Reset();
            Reset?.Reset();
            Cancel!.Reset();
            if (ApplyDelay > 0)
            {
                while (_delayedApplies!.TryDequeue(out var callback))
                    callback.Cancel();
            }
        }

        public void StartDelayedCallback(DelayedCallback callback, bool checkEnd = false, bool refresh = true)
        {
            if (Parent != null && Parent.CWC != null)
                Parent.CWC.StartDelayedCallback(callback, checkEnd, refresh);
            else
                callback.Start(checkEnd, refresh);
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "activate":
                case "triggers":
                case "trigger":
                    Activate.DeserializeTriggers(ref reader);
                    return;
                case "consumethresholdonactivate":
                case "activateconsumethreshold":
                case "consumethreshold":
                    ConsumeThresholdOnActivate = reader.GetBoolean();
                    return;
                case "applydelay":
                    ApplyDelay = reader.GetSingle();
                    if (ApplyDelay > 0f)
                        _delayedApplies = new();
                    return;
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
                case "resetdelay":
                    ResetDelay = reader.GetSingle();
                    if (ResetDelay > 0f)
                        _delayedReset = new(ResetDelay, ForceReset);
                    return;
                case "resetpreviousonly":
                case "resetprevious":
                    ResetPreviousOnly = reader.GetBoolean();
                    return;
                case "canceldelay":
                    CancelDelay = reader.GetSingle();
                    if (CancelDelay > 0f)
                        _delayedCancel = new(CancelDelay, DoCancel);
                    return;
            }

            if (property.StartsWith("apply"))
            {
                Apply ??= new(this, true, () => _applyCount = 0);
                if (property == "apply")
                    Apply.DeserializeTriggers(ref reader);
                else
                    Apply.DeserializeProperty(property[5..], ref reader);
            }
            else if (property.StartsWith("reset"))
            {
                Reset ??= new(this, false);
                if (property == "reset")
                    Reset.DeserializeTriggers(ref reader);
                else
                    Reset.DeserializeProperty(property[5..], ref reader);
            }
            else if (property.StartsWith("cancel"))
            {
                Cancel ??= new(this, false);
                if (property == "cancel")
                    Cancel.DeserializeTriggers(ref reader);
                else
                    Cancel.DeserializeProperty(property[5..], ref reader);
            }
            else
            {
                // Activate is the default and doesn't require a prefix for its fields
                if (property.StartsWith("activate"))
                    property = property[8..];
                Activate.DeserializeProperty(property, ref reader);
            }
        }

        public static TriggerCoordinator? Deserialize(ref Utf8JsonReader reader, bool allowEmptyActivate = false)
        {
            var coordinator = JSON.EWCJson.Deserialize<TriggerCoordinator>(ref reader);
            if (coordinator == null) return null;
            return allowEmptyActivate || coordinator.Activate.Triggers.Count != 0 ? coordinator : null;
        }
    }
}
