using System;
using System.Collections.Generic;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Linq;
using System.Text.Json;
using EWC.JSON;
using UnityEngine;
using EWC.Utils;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class TriggerCoordinator
    {
        private static readonly System.Random Random = new();
        public ITriggerCallback? Parent { get; set; }
        public List<ITrigger> Activate { get; private set; }
        public float Cap { get; private set; } = 0f;
        public float Threshold { get; private set; } = 0f;
        public float Cooldown { get; private set; } = 0f;
        public float CooldownThreshold { get; private set; } = 0f;
        public float Chance { get; private set; } = 1f;
        public float ActivateResetDelay { get; private set; } = 0f;

        public List<ITrigger>? Apply { get; private set; }
        public float ApplyDelay { get; private set; } = 0f;
        public float CooldownOnApply { get; private set; } = 0f;
        public uint CooldownOnApplyThreshold { get; private set; } = 1;
        public float ApplyResetDelay { get; private set; } = 0f;
        public float ApplyOverrideAmount { get; private set; } = 0f;

        public List<ITrigger>? Reset { get; private set; }
        public float ResetDelay { get; private set; } = 0f;
        public bool ResetPreviousOnly { get; private set; } = false;
        public float ResetResetDelay { get; private set; } = 0f;

        private uint _applyCount = 0;
        private float _activateSum = 0f;
        private float _activateCount = 0f;
        private float _nextActivateTime = 0f;
        private readonly List<TriggerContext> _accumulatedTriggers = new();
        private Queue<(DelayedCallback, List<TriggerContext>)>? _delayedApplies;

        private readonly DelayedCallback _activateReset;
        private readonly DelayedCallback _applyReset;
        private readonly DelayedCallback _resetReset;

        public TriggerCoordinator(params ITrigger[] triggers)
        {
            Activate = new(triggers);

            _activateReset = new DelayedCallback(
                () => ActivateResetDelay,
                () => 
                {
                    _accumulatedTriggers.Clear();
                    _activateCount = 0;
                    _activateSum = 0;
                    Activate.ForEach(trigger => trigger.Reset());
                }
                );

            _applyReset = new DelayedCallback(
                () => ApplyResetDelay,
                () =>
                {
                    _applyCount = 0;
                    Apply!.ForEach(trigger => trigger.Reset());
                }
                );

            _resetReset = new DelayedCallback(
                () => ResetResetDelay,
                () =>
                {
                    Reset!.ForEach(trigger => trigger.Reset());
                }
                );
        }

        public TriggerCoordinator Clone()
        {
            TriggerCoordinator result = new()
            {
                Activate = CloneTriggers(Activate)!,
                Apply = CloneTriggers(Apply),
                Reset = CloneTriggers(Reset),
                Cap = Cap,
                Threshold = Threshold,
                Cooldown = Cooldown,
                CooldownThreshold = CooldownThreshold,
                Chance = Chance,
                ActivateResetDelay = ActivateResetDelay,
                ApplyDelay = ApplyDelay,
                CooldownOnApply = CooldownOnApply,
                CooldownOnApplyThreshold = CooldownOnApplyThreshold,
                ApplyResetDelay = ApplyResetDelay,
                ApplyOverrideAmount = ApplyOverrideAmount,
                ResetDelay = ResetDelay,
                ResetPreviousOnly = ResetPreviousOnly,
                ResetResetDelay = ResetResetDelay,
                _delayedApplies = _delayedApplies != null ? new() : null
            };
            return result;
        }

        private List<ITrigger>? CloneTriggers(List<ITrigger>? list)
        {
            if (list == null) return null;

            return list.ConvertAll(trigger => trigger.Clone());
        }

        public void Invoke(WeaponTriggerContext context)
        {
            // Store valid activations (if any).
            // Only allow activations to go through if 1. Not on cooldown, 2. Below cap, and 3. Chance succeeds.
            if (Clock.Time >= _nextActivateTime
             && (Cap == 0 || _activateSum < Cap)
             && (Chance == 1f || Chance > Random.NextSingle()))
            {
                foreach (ITrigger trigger in Activate)
                {
                    // State-ful triggers may Invoke to trigger the reset delay but provide 0 amount.
                    // We only want to include actual triggers providing some trigger amount.
                    if (trigger.Invoke(context, out float amount))
                    {
                        if (ActivateResetDelay > 0f)
                            _activateReset.Start();
                        if (amount > 0f)
                            ActivateTrigger(context, amount);
                        if (Cap > 0 && _activateSum >= Cap) break;
                    }
                }
            }

            // Check if we will want to apply anything.
            // Necessary to check before resets for ResetPreviousOnly to function correctly.
            bool apply = false;
            if (_activateSum > 0 && _activateSum >= Threshold)
            {
                if (Apply != null)
                {
                    foreach (ITrigger trigger in Apply)
                    {
                        // State-ful trigger checks
                        if (trigger.Invoke(context, out float amount))
                        {
                            if (ApplyResetDelay > 0f)
                                _applyReset.Start();
                            apply |= amount > 0f;
                        }
                    }
                }
                else
                    apply = true;
            }

            // Check if we will want to reset.
            // Necessary to check before applying for ResetPreviousOnly to function correctly.
            bool reset = false;
            if (Reset != null)
            {
                foreach (ITrigger trigger in Reset)
                {
                    // State-ful trigger checks
                    if (trigger.Invoke(context, out float amount))
                    {
                        if (ResetResetDelay > 0f)
                            _resetReset.Start();
                        reset |= amount > 0f;
                    }
                }
            }

            // Similar to a standard reset, but fields needed to apply stored activations are preserved
            if (ResetPreviousOnly && reset)
                ResetTriggers(!apply);

            // Apply stored activations. If there are no Apply triggers, stored activations apply immediately
            if (apply)
                ApplyTriggers();

            // Reset stored activations AND any related behavior on the callback this coordinator is tied to
            if (!ResetPreviousOnly && reset)
                ResetTriggers();
        }

        private void ActivateTrigger(WeaponTriggerContext context, float triggerAmt)
        {
            _accumulatedTriggers.Add(new TriggerContext { triggerAmt = triggerAmt, context = context });
            _activateCount += triggerAmt;
            _activateSum += triggerAmt;

            if (_activateCount >= CooldownThreshold)
            {
                _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + Cooldown);
                _activateCount = 0f;
            }
        }

        private void ApplyTriggers()
        {
            // Need to copy list and reset prior to applying trigger in case applying triggers causes another application
            List<TriggerContext> temp;
            if (ApplyOverrideAmount > 0f)
            {
                temp = new((int) ApplyOverrideAmount);
                float total = 0f;
                // Iterates backwards so most recent triggers are prioritized for more reactive behavior.
                // Only last trigger is adjusted to account for underflow or overflow since it's the simplest solution.
                for (int i = _accumulatedTriggers.Count - 1; i >= 0 && total < ApplyOverrideAmount; i--)
                {
                    total += _accumulatedTriggers[i].triggerAmt;
                    temp.Add(_accumulatedTriggers[i]);
                }

                if (total != ApplyOverrideAmount)
                {
                    var tContext = temp[^1];
                    tContext.triggerAmt += ApplyOverrideAmount - total;
                    temp[^1] = tContext;
                }
            }
            else
                temp = new(_accumulatedTriggers);

            _accumulatedTriggers.Clear();
            _activateSum = 0f;

            if (++_applyCount >= CooldownOnApplyThreshold)
            {
                _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + CooldownOnApply);
                _applyCount = 0;
            }

            DoApply(temp);
        }

        private void DoApply(List<TriggerContext> triggerContexts)
        {
            if (ApplyDelay > 0f)
            {
                var callback = new DelayedCallback(ApplyDelay, EndDelayedApply);
                _delayedApplies!.Enqueue((callback, triggerContexts));
                callback.Start();
            }
            else
                Parent?.TriggerApply(triggerContexts);
        }

        private void EndDelayedApply()
        {
            (_, List<TriggerContext> contexts) = _delayedApplies!.Dequeue();
            Parent?.TriggerApply(contexts);
        }

        private void ResetTriggers(bool resetAccumulated = true)
        {
            if (ResetDelay > 0f)
                new DelayedCallback(ResetDelay, EndDelayedReset).Start();
            else
                DoReset(resetAccumulated);
        }

        private void DoReset(bool resetAccumulated = true)
        {
            if (resetAccumulated)
            {
                _accumulatedTriggers.Clear();
                _applyCount = 0;
                _applyReset.Cancel();
                Apply?.ForEach(trigger => trigger.Reset());
            }

            Parent?.TriggerReset();
            _activateCount = 0;
            _activateSum = 0f;
            _activateReset.Cancel();
            Activate.ForEach(trigger => trigger.Reset());

            if (ApplyDelay > 0)
            {
                while (_delayedApplies!.TryDequeue(out (DelayedCallback callback, List<TriggerContext>) pair))
                    pair.callback.Cancel();
            }
        }

        private void EndDelayedReset() => DoReset(true);

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "threshold":
                    Threshold = Math.Max(reader.GetSingle(), 0);
                    break;
                case "cooldown":
                    Cooldown = reader.GetSingle();
                    break;
                case "cooldownthreshold":
                    CooldownThreshold = Math.Max(reader.GetSingle(), 0);
                    break;
                case "activateresetdelay":
                    ActivateResetDelay = reader.GetSingle();
                    break;
                case "chance":
                    Chance = reader.GetSingle();
                    break;
                case "applydelay":
                    ApplyDelay = reader.GetSingle();
                    if (ApplyDelay > 0f)
                        _delayedApplies = new();
                    break;
                case "cooldownonapply":
                    CooldownOnApply = reader.GetSingle();
                    break;
                case "cooldownonapplythreshold":
                    CooldownOnApplyThreshold = Math.Max(reader.GetUInt32(), 1);
                    break;
                case "applyresetdelay":
                    ApplyResetDelay = reader.GetSingle();
                    break;
                case "applyoverrideamount":
                case "applyamount":
                    ApplyOverrideAmount = reader.GetSingle();
                    break;
                case "activate":
                case "triggers":
                case "trigger":
                case "name":
                    List<ITrigger>? triggers = DeserializeTriggers(ref reader)!;
                    if (triggers != null)
                        Activate.AddRange(triggers);
                    break;
                case "apply":
                    Apply = DeserializeTriggers(ref reader);
                    break;
                case "reset":
                    Reset = DeserializeTriggers(ref reader);
                    break;
                case "resetdelay":
                    ResetDelay = reader.GetSingle();
                    break;
                case "resetpreviousonly":
                case "resetprevious":
                    ResetPreviousOnly = reader.GetBoolean();
                    break;
                case "resetresetdelay":
                    ResetResetDelay = reader.GetSingle();
                    break;
            }
        }

        private static List<ITrigger>? DeserializeTriggers(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.StartObject)
            {
                ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                if (trigger == null) return null;
                return new List<ITrigger>() { trigger };
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<ITrigger> result = new();
                reader.Read();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                    reader.Read();
                    if (trigger == null) return null;
                    result.Add(trigger);
                }
                if (!result.Any()) return null;
                return result;
            }

            throw new JsonException("Expected trigger or list of triggers when deserializing triggers for Coordinator");
        }
    }
}
