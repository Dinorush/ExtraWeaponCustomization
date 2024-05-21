using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class TriggerMod :
        IWeaponProperty<WeaponTriggerContext>
    {
        public bool AllowStack { get; } = true;

        public float Mod { get; set; } = 1f;
        public float Cap { get; set; } = -1f;
        public float Duration { get; set; } = 0f;
        public StackType StackType { get; set; } = StackType.None;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;
        public TriggerType ResetTriggerType { get; set; } = TriggerType.Invalid;

        public void Invoke(WeaponTriggerContext context)
        {
            if (context.Type.IsType(ResetTriggerType))
            {
                Reset();
                return;
            }
            else if (!context.Type.IsType(TriggerType)) return;

            AddStack(context);
        }

        private float ClampToCap(float mod)
        {
            if (Cap < 0) return mod;
            if (Cap > 1f) return Math.Min(mod, Cap);
            return Math.Max(mod, Cap);
        }

        protected float CalculateMod(int count)
        {
            return ClampToCap(
                StackType switch
                {
                    StackType.Multiply or StackType.None => (float)Math.Pow(Mod, count),
                    StackType.Add => Math.Max(0f, 1f + (Mod - 1f) * count),
                    _ => 1f
                }
                );
        }

        public abstract void Reset();
        public abstract void AddStack(WeaponTriggerContext context);
        public abstract IWeaponProperty Clone();
        protected void CopyFrom(TriggerMod triggerMod)
        {
            Mod = triggerMod.Mod;
            Cap = triggerMod.Cap;
            Duration = triggerMod.Duration;
            StackType = triggerMod.StackType;
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
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
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
                case "stacktype":
                case "stack":
                    StackType = reader.GetString()?.ToStackType() ?? StackType.Invalid;
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
    }
}
