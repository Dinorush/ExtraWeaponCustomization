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
        public readonly List<ITrigger> Activate;
        public uint Cap { get; private set; } = 0;
        public uint Threshold { get; private set; } = 1;
        public float Cooldown { get; private set; } = 0f;
        public uint CooldownThreshold { get; private set; } = 1;
        public float Chance { get; private set; } = 1f;
        public float ActivateResetDelay { get; private set; } = 0f;

        public List<ITrigger>? Apply { get; private set; }
        public float CooldownOnApply { get; private set; } = 0f;
        public uint CooldownOnApplyThreshold { get; private set; } = 1;
        public float ApplyResetDelay { get; private set; } = 0f;

        public List<ITrigger>? Reset { get; private set; }
        public bool ResetPreviousOnly { get; private set; } = false;

        private uint _applyCount = 0;
        private float _nextActivateTime = 0f;
        private readonly List<TriggerContext> _accumulatedTriggers = new();

        private float _activateResetTime = 0f;
        private readonly DelayedCallback _activateReset;
        private float _applyResetTime = 0f;
        private readonly DelayedCallback _applyReset;

        public TriggerCoordinator(params ITrigger[] triggers)
        {
            _activateReset = new DelayedCallback(
                () => _activateResetTime,
                null,
                () => _activateResetTime = ActivateResetDelay + Clock.Time,
                () => _accumulatedTriggers.Clear()
                );

            _applyReset = new DelayedCallback(
                () => _applyResetTime,
                null,
                () => _applyResetTime = ApplyResetDelay + Clock.Time,
                () => _applyCount = 0
                );

            Activate = new(triggers);
        }

        public TriggerCoordinator Clone()
        {
            // Only need to shallow copy the triggers themselves since they hold no state information
            TriggerCoordinator result = new()
            {
                Apply = Apply != null ? new(Apply) : null,
                Reset = Reset != null ? new(Reset) : null,
                Cap = Cap,
                Threshold = Threshold,
                Chance = Chance,
                Cooldown = Cooldown,
                CooldownThreshold = CooldownThreshold,
                ActivateResetDelay = ActivateResetDelay,
                CooldownOnApply = CooldownOnApply,
                CooldownOnApplyThreshold = CooldownOnApplyThreshold,
                ApplyResetDelay = ApplyResetDelay,
                ResetPreviousOnly = ResetPreviousOnly
            };
            result.Activate.AddRange(Activate);
            return result;
        }

        public void Invoke(WeaponTriggerContext context)
        {
            // Store valid activations (if any)
            if (Clock.Time >= _nextActivateTime
             && (Cap == 0 || _accumulatedTriggers.Count < Cap)
             && (Chance == 1f || Chance > Random.NextSingle()))
            {
                foreach (ITrigger trigger in Activate)
                {
                    float triggerAmt = trigger.Invoke(context);
                    if (triggerAmt > 0f)
                    {
                        ActivateTriggers(context, triggerAmt);
                        if (Cap > 0 && _accumulatedTriggers.Count == Cap) break;
                    }
                }
            }

            bool reset = Reset?.Any(trigger => trigger.Invoke(context) > 0) == true;
            bool apply = _accumulatedTriggers.Count >= Threshold && (Apply?.Any(trigger => trigger.Invoke(context) > 0) ?? true);
            if (ResetPreviousOnly && reset)
                ResetTriggers(!apply);

            // Apply stored activations. If there are no Apply triggers, stored activations apply immediately
            if (apply)
                ApplyTriggers();

            // Reset stored activations AND any related behavior on the callback this coordinator is tied to
            if (!ResetPreviousOnly && reset)
                ResetTriggers();
        }

        private void ActivateTriggers(WeaponTriggerContext context, float triggerAmt)
        {
            _accumulatedTriggers.Add(new TriggerContext { triggerAmt = triggerAmt, context = context });
            
            if (_accumulatedTriggers.Count % CooldownThreshold == 0)
                _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + Cooldown);

            if (ActivateResetDelay > 0f)
                _activateReset.Start();
        }

        private void ApplyTriggers()
        {
            // Need to copy list and reset prior to applying trigger in case applying triggers causes another application
            List<TriggerContext> temp = new(_accumulatedTriggers);
            _accumulatedTriggers.Clear();
            if (ActivateResetDelay > 0f)
                _activateReset.Cancel();

            Parent?.TriggerApply(temp);

            if (_applyCount == CooldownOnApplyThreshold)
            {
                _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + CooldownOnApply);
                _applyCount = 0;
            }

            if (ApplyResetDelay > 0f)
                _applyReset.Start();
        }

        private void ResetTriggers(bool resetAccumulated = true)
        {
            if (resetAccumulated)
                _accumulatedTriggers.Clear();
            Parent?.TriggerReset();
            _activateReset.Cancel();
            _applyReset.Cancel();
            _applyCount = 0;
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "cap":
                    Cap = reader.GetUInt32();
                    break;
                case "threshold":
                    Threshold = Math.Max(reader.GetUInt32(), 1);
                    break;
                case "cooldown":
                    Cooldown = reader.GetSingle();
                    break;
                case "cooldownthreshold":
                    CooldownThreshold = Math.Max(reader.GetUInt32(), 1);
                    break;
                case "activateresetdelay":
                    ActivateResetDelay = reader.GetSingle();
                    break;
                case "chance":
                    Chance = reader.GetSingle();
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
                case "activate":
                case "triggers":
                case "trigger":
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
                case "resetpreviousonly":
                case "resetprevious":
                    ResetPreviousOnly = reader.GetBoolean();
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
