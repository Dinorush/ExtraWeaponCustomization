using System;
using System.Collections.Generic;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Linq;
using System.Text.Json;
using EWC.JSON;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class TriggerCoordinator
    {
        private static readonly Random Random = new();
        public ITriggerCallback? Parent { get; set; }
        public List<ITrigger> Activate { get; private set; }
        public uint Cap { get; private set; } = 0;
        public uint Threshold { get; private set; } = 1;
        public float Cooldown { get; private set; } = 0f;
        public float Chance { get; private set; } = 1f;

        public List<ITrigger>? Apply { get; private set; }
        public float CooldownOnApply { get; private set; } = 0f;

        public List<ITrigger>? Reset { get; private set; }

        private float _nextActivateTime = 0f;
        private readonly List<TriggerContext> _accumulatedTriggers = new();

        public TriggerCoordinator(params ITrigger[] triggers)
        {
            Activate = new(triggers);
        }

        public TriggerCoordinator Clone()
        {
            // Only need to shallow copy the triggers themselves since they hold no state information
            TriggerCoordinator result = new()
            {
                Activate = new(Activate),
                Apply = Apply != null ? new(Apply) : null,
                Reset = Reset != null ? new(Reset) : null,
                Cap = Cap,
                Cooldown = Cooldown,
                Chance = Chance,
                CooldownOnApply = CooldownOnApply
            };
            return result;
        }

        public void Invoke(WeaponTriggerContext context)
        {
            // Store valid activations (if any)
            if (Clock.Time >= _nextActivateTime
             && (Cap == 0 || _accumulatedTriggers.Count < Cap)
             && (Chance == 1f || Chance > Random.NextDouble()))
            {
                foreach (ITrigger trigger in Activate)
                {
                    float triggerAmt = trigger.Invoke(context);
                    if (triggerAmt > 0f)
                    {
                        _accumulatedTriggers.Add(new TriggerContext { triggerAmt = triggerAmt, context = context });
                        _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + Cooldown);
                        if (Cap > 0 && _accumulatedTriggers.Count == Cap) break;
                    }
                }
            }

            // Apply stored activations. If there are no Apply triggers, stored activations apply immediately
            if (_accumulatedTriggers.Count >= Threshold && (Apply?.Any(trigger => trigger.Invoke(context) > 0) ?? true))
                ApplyTriggers();

            // Reset stored activations AND any related behavior on the callback this coordinator is tied to
            if (Reset?.Any(trigger => trigger.Invoke(context) > 0) == true)
                ResetTriggers();
        }

        private void ApplyTriggers()
        {
            // Need to copy list and reset prior to applying trigger in case applying triggers causes another application
            List<TriggerContext> temp = new(_accumulatedTriggers);
            _accumulatedTriggers.Clear();
            Parent?.TriggerApply(temp);
            _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + CooldownOnApply);
        }

        private void ResetTriggers()
        {
            _accumulatedTriggers.Clear();
            Parent?.TriggerReset();
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
                case "chance":
                    Chance = reader.GetSingle();
                    break;
                case "cooldownonapply":
                    CooldownOnApply = reader.GetSingle();
                    break;
                case "activate":
                case "triggers":
                case "trigger":
                    // Can actually be null here, but converter will return a null if this is.
                    Activate = DeserializeTriggers(ref reader)!;
                    break;
                case "apply":
                    Apply = DeserializeTriggers(ref reader);
                    break;
                case "reset":
                    Reset = DeserializeTriggers(ref reader);
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
