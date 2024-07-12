using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class TriggerMod :
        IWeaponProperty<WeaponTriggerContext>
    {
        public bool AllowStack { get; } = true;

        public float Mod { get; set; } = 1f;
        public float Cap { get; set; } = 0f;
        public float Duration { get; set; } = 0f;
        public float Cooldown { get; set; } = 0f;
        public StackType StackType { get; set; } = StackType.Add;
        public StackType StackLayer { get; set; } = StackType.Multiply;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;
        public TriggerType ResetTriggerType { get; set; } = TriggerType.Invalid;

        private float _lastStackTime = 0f;

        public void Invoke(WeaponTriggerContext context)
        {
            if (context.Type.IsType(ResetTriggerType))
            {
                Reset();
                return;
            }
            else if (!context.Type.IsType(TriggerType) || Clock.Time < _lastStackTime + Cooldown) return;

            AddStack(context);
            _lastStackTime = Clock.Time;
        }

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
                    _ => 1f
                }
                );
        }

        protected float CalculateOnDamageMod(float damage)
        {
            return StackType switch
            {
                StackType.None => Mod,
                StackType.Multiply => (float) Math.Pow(Mod, damage),
                StackType.Add => 1f + (Mod - 1f) * damage,
                _ => 1f
            };
        }

        public abstract void Reset();
        public abstract void AddStack(WeaponTriggerContext context);
        public abstract IWeaponProperty Clone();
        protected void CopyFrom(TriggerMod triggerMod)
        {
            Mod = triggerMod.Mod;
            Cap = triggerMod.Cap;
            Duration = triggerMod.Duration;
            Cooldown = triggerMod.Cooldown;
            StackType = triggerMod.StackType;
            StackLayer = triggerMod.StackLayer;
            TriggerType = triggerMod.TriggerType;
            ResetTriggerType = triggerMod.ResetTriggerType;
        }

        public abstract void WriteName(Utf8JsonWriter writer, JsonSerializerOptions options);

        public virtual void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteName(writer, options);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteString(nameof(ResetTriggerType), ResetTriggerType.ToString());
            writer.WriteEndObject();
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
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
                case "cooldown":
                    Cooldown = reader.GetSingle();
                    break;
                case "stacktype":
                case "stack":
                    StackType = reader.GetString()?.ToStackType() ?? StackType.Invalid;
                    break;
                case "stacklayer":
                case "layer":
                    StackLayer = reader.GetString()?.ToStackType() ?? StackType.Invalid;
                    break;
                case "triggertype":
                case "trigger":
                    TriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    break;
                case "resettriggertype":
                case "resettrigger":
                    ResetTriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
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
        };
    }
}
