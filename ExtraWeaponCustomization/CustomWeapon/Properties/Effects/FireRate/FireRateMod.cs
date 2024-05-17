﻿using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using ExtraWeaponCustomization.Utils;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public class FireRateMod : IWeaponProperty<WeaponTriggerContext>, IWeaponProperty<WeaponFireRateContext>
    {
        public readonly static string Name = typeof(FireRateMod).Name;
        public bool AllowStack { get; } = true;

        public float Mod { get; set; } = 1f;
        public float Duration { get; set; } = 0f;
        public StackType StackType { get; set; } = StackType.None;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;

        private readonly Queue<float> _expireTimes = new();

        public void Invoke(WeaponTriggerContext context)
        {
            if (context.Type != TriggerType) return;

            if (StackType == StackType.None) _expireTimes.Clear();
            _expireTimes.Enqueue(Clock.Time + Duration);
        }

        public void Invoke(WeaponFireRateContext context)
        {
            while (_expireTimes.TryPeek(out float time) && time < Clock.Time) _expireTimes.Dequeue();
            context.FireRate *= StackType.CalculateMod(Mod, _expireTimes.Count);
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "mod":
                    Mod = reader.GetSingle();
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
                default:
                    break;
            }
        }
    }
}
