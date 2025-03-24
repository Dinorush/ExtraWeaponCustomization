using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class TriggerHolder
    {
        private static readonly Random Random = new();

        public List<ITrigger> Triggers { get; private set; }
        public float Cap { get; private set; } = 0f;
        public float Threshold { get; private set; } = 0f;
        public float Cooldown { get; private set; } = 0f;
        public float CooldownThreshold { get; private set; } = 0f;
        public float Chance { get; private set; } = 1f;
        public float ResetDelay { get; private set; } = 0f;

        private readonly TriggerCoordinator _parent;
        private readonly bool _implyTrigger;
        private readonly DelayedCallback _delayedReset;
        private float _triggerSum = 0f;
        private float _triggerCount = 0f;
        private float _nextTriggerTime = 0f;

        public TriggerHolder(TriggerCoordinator parent, bool implyTrigger, Action? onDelayedReset = null, params ITrigger[] triggers)
        {
            _parent = parent;
            Triggers = new(triggers);
            _implyTrigger = implyTrigger;
            _delayedReset = new DelayedCallback(
                () => ResetDelay,
                () =>
                {
                    _triggerSum = 0;
                    _triggerCount = 0;
                    Triggers.ForEach(trigger => trigger.Reset());
                    onDelayedReset?.Invoke();
                }
                );
        }

        public bool Invoke(WeaponTriggerContext context, in List<TriggerContext>? accumulatedList = null)
        {
            if (Triggers.Count == 0)
            {
                if (_implyTrigger)
                    AddTrigger(context, 1f, accumulatedList);
                return _triggerSum > 0 && _triggerSum >= Threshold;
            }

            if (Clock.Time >= _nextTriggerTime
             && (Cap == 0 || _triggerSum < Cap)
             && (Chance == 1f || Chance > Random.NextSingle()))
            {
                foreach (ITrigger trigger in Triggers)
                {
                    // State-ful triggers may Invoke to trigger the reset delay but provide 0 amount.
                    // We only want to include actual triggers providing some trigger amount.
                    if (trigger.Invoke(context, out float amount))
                    {
                        if (ResetDelay > 0f)
                            _parent.StartDelayedCallback(_delayedReset, checkEnd: true);
                        if (amount > 0f)
                            AddTrigger(context, amount, accumulatedList);
                        if (Cap > 0 && _triggerSum >= Cap) break;
                    }
                }
            }
            
            return _triggerSum > 0 && _triggerSum >= Threshold;
        }

        private void AddTrigger(WeaponTriggerContext context, float triggerAmt, in List<TriggerContext>? accumulatedList = null)
        {
            accumulatedList?.Add(new TriggerContext { triggerAmt = triggerAmt, context = context });
            _triggerCount += triggerAmt;
            _triggerSum += triggerAmt;

            if (_triggerCount >= CooldownThreshold)
            {
                _nextTriggerTime = Math.Max(_nextTriggerTime, Clock.Time + Cooldown);
                _triggerCount = 0f;
            }
        }

        public void SetCooldown(float time) => _nextTriggerTime = Math.Max(Clock.Time + time, _nextTriggerTime);

        public void ClearAccumulated()
        {
            _triggerSum = 0f;
        }

        public void ReduceAccumulated()
        {
            _triggerSum -= Threshold;
        }

        public void Reset()
        {
            _triggerSum = 0f;
            _triggerCount = 0f;
            _delayedReset.Cancel();
            foreach (var trigger in Triggers)
                trigger.Reset();
        }

        public TriggerHolder Clone(TriggerCoordinator parent, Action? onDelayedReset = null)
        {
            TriggerHolder copy = CopyUtil<TriggerHolder>.Clone(this, parent, _implyTrigger, onDelayedReset);
            copy.Triggers = Triggers.ConvertAll(trigger => trigger.Clone());
            return copy;
        }

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
                case "chance":
                    Chance = reader.GetSingle();
                    break;
                case "resetdelay":
                    ResetDelay = reader.GetSingle();
                    break;
            }
        }

        public void DeserializeTriggers(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.StartObject)
            {
                ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                if (trigger == null) return;
                Triggers.Add(trigger);
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                    reader.Read();
                    if (trigger == null) return;
                    Triggers.Add(trigger);
                }
            }
            else
                throw new JsonException("Expected trigger or list of triggers when deserializing triggers for Coordinator");
        }
    }
}
