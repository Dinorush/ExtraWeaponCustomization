using EWC.CustomWeapon.Enums;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public abstract class TriggerMod :
        Effect
    {
        public float Mod { get; private set; } = 1f;
        public float Cap { get; private set; } = 0f;
        public float Duration { get; private set; } = 0f;
        public bool CombineModifiers { get; private set; } = false;
        public float CombineDecayTime { get; private set; } = 0f;
        public StackType StackType { get; private set; } = StackType.Add;
        public StackType OverrideStackType { get; private set; } = StackType.Override;
        public StackType StackLayer { get; private set; } = StackType.Multiply;

        private float ClampToCap(float mod)
        {
            if (Cap > 1f) return Math.Min(mod, Cap);
            return Math.Max(mod, Cap);
        }

        protected float CalculateMod(IEnumerable<TriggerInstance> count, bool clamped = true)
        {
            if (!count.Any()) return 1f;

            float result = StackType switch
            {
                StackType.None => CalculateMod(count.First().triggerAmt),
                StackType.Multiply or StackType.Add => CalculateMod(Sum(count)),
                StackType.Max or StackType.Min => CalculateMod(Mod > 1f ? count.Max(x => x.triggerAmt) : count.Min(x => x.triggerAmt)),
                _ => 1f
            };
            return clamped ? ClampToCap(result) : result;
        }

        protected float CalculateMod(float num, bool clamped = true) => CalculateMod(StackType == StackType.Override ? OverrideStackType : StackType, num, clamped);

        protected float CalculateMod(StackType type, float num, bool clamped = true)
        {
            float result = type switch
            {
                StackType.None => Mod,
                StackType.Multiply => (float)Math.Pow(Mod, num),
                StackType.Add => 1f + (Mod - 1f) * num,
                _ => 1f
            };
            return clamped ? ClampToCap(result) : result;
        }

        protected static float Sum(IEnumerable<TriggerContext> contexts) => contexts.Sum(context => context.triggerAmt);
        protected static float Sum(IEnumerable<TriggerInstance> contexts) => contexts.Sum(context => context.triggerAmt);

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "mod":
                    Mod = reader.GetSingle();
                    break;
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "combinemodifiers":
                case "combine":
                    CombineModifiers = reader.GetBoolean();
                    break;
                case "combinedecaytime":
                case "combinedecay":
                case "decaytime":
                case "decay":
                    CombineDecayTime = reader.GetSingle();
                    break;
                case "stacktype":
                case "stack":
                    StackType = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                case "overridestacktype":
                case "overridestack":
                    OverrideStackType = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                case "stacklayer":
                case "layer":
                    StackLayer = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                default:
                    break;
            }
        }

        protected struct TriggerInstance
        {
            public float triggerAmt = 1f;
            public float endTime = 0f;

            public TriggerInstance(float triggerAmt, float endTime)
            {
                this.triggerAmt = triggerAmt;
                this.endTime = endTime;
            }
        }

        protected class TriggerStack
        {
            private float _currentStacks = 0f;
            private float _lastStackTime = 0f;
            private float _lastUpdateTime = 0f;
            private readonly Queue<TriggerInstance> _expireTimes = new();
            private readonly TriggerMod _parent;

            public TriggerStack(TriggerMod parent) => _parent = parent;

            public void Clear()
            {
                _expireTimes.Clear();
                _currentStacks = 0f;
            }

            public void Add(List<TriggerContext> contexts) => Add(Sum(contexts));
            public void Add(float num)
            {
                if (_parent.CombineModifiers)
                {
                    RefreshStackMod();
                    _lastStackTime = Clock.Time;
                    if (_parent.StackType == StackType.Override)
                        _currentStacks = num;
                    else
                        _currentStacks += num;
                    return;
                }

                if (_parent.StackType == StackType.None)
                    _expireTimes.Clear();

                float endTime = Clock.Time + _parent.Duration;
                _expireTimes.Enqueue(new TriggerInstance(num, endTime));
            }

            public bool TryGetMod(out float mod)
            {
                mod = 1f;
                if (_parent.CombineModifiers)
                {
                    if (_currentStacks == 0f) return false;
                    RefreshStackMod();
                    if (_currentStacks == 0f) return false;
                    mod = _parent.CalculateMod(_currentStacks);
                    return true;
                }

                while (_expireTimes.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) _expireTimes.Dequeue();

                if (_expireTimes.Count == 0) return false;
                mod = _parent.CalculateMod(_expireTimes);
                return true;
            }

            private void RefreshStackMod()
            {
                float time = Clock.Time;
                float decayTime = _lastStackTime + _parent.Duration;
                if (decayTime > time)
                    return;
                else if (decayTime > _lastUpdateTime)
                    _lastUpdateTime = decayTime;
                
                float decayDelta = time - _lastUpdateTime;
                if (decayDelta > 0f)
                {
                    if (_parent.CombineDecayTime <= 0f)
                        _currentStacks = 0f;
                    else
                        _currentStacks = Math.Max(0f, _currentStacks - decayDelta / _parent.CombineDecayTime);
                }
                _lastUpdateTime = time;
            }
        }
    }
}
