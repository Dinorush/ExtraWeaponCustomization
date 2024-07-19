using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class TriggerMod :
        Effect
    {
        public float Mod { get; set; } = 1f;
        public float Cap { get; set; } = 0f;
        public float Duration { get; set; } = 0f;
        public float Cooldown { get; set; } = 0f;
        public StackType StackType { get; set; } = StackType.Add;
        public StackType StackLayer { get; set; } = StackType.Multiply;

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

        protected float ConvertTriggersToMod(IEnumerable<TriggerContext> count)
        {
            if (!count.Any()) return 1f;

            float mod = 1f;
            foreach(TriggerContext context in count)
            {
                float triggerMod = CalculateMod(context.triggerAmt);
                mod = StackType switch
                {
                    StackType.None => triggerMod,
                    StackType.Multiply => mod *= triggerMod,
                    StackType.Add => mod += triggerMod - 1f,
                    _ => mod
                };
            }
            return mod;
        }

        protected void CopyFrom(TriggerMod triggerMod)
        {
            Mod = triggerMod.Mod;
            Cap = triggerMod.Cap;
            Duration = triggerMod.Duration;
            StackType = triggerMod.StackType;
            StackLayer = triggerMod.StackLayer;
            Trigger = triggerMod.Trigger?.Clone();
        }

        public abstract void WriteName(Utf8JsonWriter writer, JsonSerializerOptions options);

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteName(writer, options);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            SerializeTrigger(writer, options);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            base.DeserializeProperty(property, ref reader, options);
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
                    EWCLogger.Warning(
                        "\"Cooldown\" as an Effect field is deprecated and will not be supported in a future version." +
                        "Please port it to the Trigger object."
                        );
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

            // Backwards compatibility | Remove when Effect's Cooldown support is removed
            if (Trigger != null && Cooldown != 0)
            {
                Trigger.Cooldown = Cooldown;
                Cooldown = 0;
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
