using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public abstract class TriggerHolder
    {
        private static readonly Random Random = new();

        public List<ITrigger> Triggers { get; private set; }
        public float Cap { get; private set; } = 0f;
        public float Threshold { get; private set; } = 0f;
        public float Cooldown { get; private set; } = 0f;
        public float CooldownThreshold { get; private set; } = 0f;
        public float Chance { get; private set; } = 1f;
        public float ApplyDelay { get; private set; } = 0f;
        public float ResetDelay { get; private set; } = 0f;
        public bool ConsumeThreshold { get; private set; } = false;

        public List<ITrigger>? Apply { get; private set; }

        public List<ITrigger>? Cancel { get; private set; }
        public float CancelReduceAmount { get; private set; } = 0f;

        public readonly TriggerCoordinator Parent;
        public ITriggerCallback? Caller => Parent.Parent;
        private readonly DelayedCallback _delayedReset;

        protected float _triggerSum = 0f;
        protected float _triggerCount = 0f;
        protected float _nextTriggerTime = 0f;

        protected Queue<DelayedCallback>? _delayedApplies;

        public TriggerHolder(TriggerCoordinator parent, params ITrigger[] triggers)
        {
            Parent = parent;
            Triggers = new(triggers);
            _delayedReset = new DelayedCallback(
                () => ResetDelay,
                DelayedReset
                );
        }

        public TriggerHolder Clone(TriggerCoordinator parent)
        {
            TriggerHolder copy = CopyUtil<TriggerHolder>.Clone(this, parent);
            copy.Triggers = Triggers.ConvertAll(trigger => trigger.Clone());
            copy.Apply = Apply?.ConvertAll(trigger => trigger.Clone());
            copy.Cancel = Cancel?.ConvertAll(trigger => trigger.Clone());
            copy._delayedApplies = _delayedApplies != null ? new() : null;
            return copy;
        }

        public bool Invoke(WeaponTriggerContext context)
        {
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
                            StartDelayedCallback(_delayedReset, checkEnd: true);
                        if (amount > 0f)
                            AddTrigger(context, amount);
                        if (Cap > 0 && _triggerSum >= Cap) break;
                    }
                }
            }

            bool activate = _triggerSum > 0 && _triggerSum >= Threshold;
            if (activate)
            {
                if (ConsumeThreshold)
                    _triggerSum -= Threshold;
                if (Apply == null)
                    return true;

                foreach (ITrigger trigger in Apply)
                    if (trigger.Invoke(context, out float amount) && amount > 0)
                        return true;
            }

            if (Cancel != null)
            {
                foreach (ITrigger trigger in Cancel)
                {
                    if (trigger.Invoke(context, out float amount) && amount > 0)
                    {
                        OnCancel();
                        return false;
                    }
                }
            }

            return false;
        }

        private void AddTrigger(WeaponTriggerContext context, float triggerAmt)
        {
            OnAddTrigger(context, triggerAmt);
            _triggerCount += triggerAmt;
            _triggerSum += triggerAmt;

            if (_triggerCount >= CooldownThreshold)
            {
                SetCooldown(Cooldown);
                _triggerCount = 0f;
            }
        }

        protected void SetCooldown(float time) => _nextTriggerTime = Math.Max(Clock.Time + time, _nextTriggerTime);

        protected void ClearAccumulated()
        {
            _triggerSum = 0f;
        }

        public virtual void Reset(bool resetAccumulated = true)
        {
            _triggerSum = 0f;
            _triggerCount = 0f;
            _delayedReset.Cancel();

            Triggers.ForEach(trigger => trigger.Reset());
            Apply?.ForEach(trigger => trigger.Reset());
            Cancel?.ForEach(trigger => trigger.Reset());

            if (ApplyDelay > 0)
            {
                while (_delayedApplies!.TryDequeue(out var callback))
                    callback.Cancel();
            }
        }

        protected virtual void DelayedReset()
        {
            _triggerSum = 0;
            _triggerCount = 0;
            Triggers.ForEach(trigger => trigger.Reset());
            Apply?.ForEach(trigger => trigger.Reset());
            Cancel?.ForEach(trigger => trigger.Reset());
        }

        protected virtual void OnAddTrigger(WeaponTriggerContext context, float amount) { }

        protected virtual void OnCancel()
        {
            if (CancelReduceAmount > 0f)
                _triggerSum = Math.Max(0f, _triggerSum - CancelReduceAmount);
            else
                _triggerSum = 0f;
        }

        protected void StartDelayedCallback(DelayedCallback callback, bool checkEnd = false, bool refresh = true)
        {
            if (Caller != null && Caller.CWC != null)
                Caller.CWC.StartDelayedCallback(callback, checkEnd, refresh);
            else
                callback.Start(checkEnd, refresh);
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
                case "consumethreshold":
                    ConsumeThreshold = reader.GetBoolean();
                    break;
                case "apply":
                    Apply = DeserializeTriggers(ref reader);
                    break;
                case "applydelay":
                case "delay":
                    ApplyDelay = reader.GetSingle();
                    if (ApplyDelay > 0f)
                        _delayedApplies = new();
                    break;
                case "cancel":
                    Cancel = DeserializeTriggers(ref reader);
                    break;
                case "cancelreduceamount":
                    CancelReduceAmount = reader.GetSingle();
                    break;
            }
        }

        private static List<ITrigger>? DeserializeTriggers(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.StartObject)
            {
                ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                if (trigger == null) return null;
                return new() { trigger };
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<ITrigger> list = new();
                reader.Read();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    ITrigger? trigger = EWCJson.Deserialize<ITrigger>(ref reader);
                    reader.Read();
                    if (trigger == null) continue;
                    list.Add(trigger);
                }
                return list;
            }
            throw new JsonException("Expected trigger or list of triggers when deserializing triggers for Coordinator");
        }

        public void DeserializeTriggerList(ref Utf8JsonReader reader)
        {
            var list = DeserializeTriggers(ref reader);
            if (list != null)
                Triggers = list;
        }
    }
}
