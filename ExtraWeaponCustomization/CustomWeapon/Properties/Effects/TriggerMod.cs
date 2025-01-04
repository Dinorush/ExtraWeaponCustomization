using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public abstract class TriggerMod :
        Effect
    {
        public float Mod { get; private set; } = 1f;
        public float Cap { get; private set; } = 0f;
        public float Duration { get; private set; } = 0f;
        public float DecayTime { get; private set; } = -1f;
        public StackType StackType { get; private set; } = StackType.Add;
        public StackType StackLayer { get; private set; } = StackType.Multiply;

        public bool IsStackingMod => DecayTime >= 0f;

        private float ClampToCap(float mod)
        {
            if (Cap > 1f) return Math.Min(mod, Cap);
            return Math.Max(mod, Cap);
        }

        protected float CalculateMod(IEnumerable<TriggerInstance> count)
        {
            if (!count.Any()) return 1f;

            return ClampToCap(
                StackType switch
                {
                    StackType.None => count.First().mod,
                    StackType.Multiply => count.Aggregate(new TriggerInstance(1f, 0f), (x, y) => { x.mod *= y.mod; return x; }, x => x.mod),
                    StackType.Add => count.Aggregate(new TriggerInstance(1f, 0f), (x, y) => { x.mod += (y.mod - 1f); return x; }, x => x.mod),
                    StackType.Max => Mod > 1f ? count.Max(x => x.mod) : count.Min(x => x.mod),
                    _ => 1f
                }
                );
        }

        protected float CalculateMod(float num)
        {
            return StackType switch
            {
                StackType.None => Mod,
                StackType.Multiply => (float)Math.Pow(Mod, num),
                StackType.Add => 1f + (Mod - 1f) * num,
                _ => 1f
            };
        }

        protected static float Sum(IEnumerable<TriggerContext> contexts) => contexts.Sum(context => context.triggerAmt);

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteNumber(nameof(DecayTime), DecayTime);
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
                case "decaytime":
                case "decay":
                    DecayTime = reader.GetSingle();
                    break;
                case "stacktype":
                case "stack":
                    StackType = reader.GetString().ToEnum(StackType.Invalid);
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
            public float mod = 1f;
            public float endTime = 0f;

            public TriggerInstance(float mod, float endTime)
            {
                this.mod = mod;
                this.endTime = endTime;
            }
        }

        protected class TriggerStack
        {
            private float _currentStacks = 0f;
            private float _lastStackTime = 0f;
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
                if (_parent.IsStackingMod)
                {
                    RefreshStackMod();
                    _currentStacks += num;
                    return;
                }

                if (_parent.StackType == StackType.None)
                    _expireTimes.Clear();

                float endTime = Clock.Time + _parent.Duration;
                _expireTimes.Enqueue(new TriggerInstance(_parent.CalculateMod(num), endTime));
            }

            public bool TryGetMod(out float mod)
            {
                mod = 1f;
                if (_parent.IsStackingMod)
                {
                    if (_currentStacks == 0f) return false;
                    RefreshStackMod();
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
                float decayDelta = time - _lastStackTime - _parent.Duration;
                if (decayDelta > 0f)
                {
                    if (_parent.DecayTime == 0f)
                        _currentStacks = 0f;
                    else
                        _currentStacks = Math.Max(0f, _currentStacks - decayDelta / _parent.DecayTime);
                }
                _lastStackTime = time;
            }
        }
    }
}
