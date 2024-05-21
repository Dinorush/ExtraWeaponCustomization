using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public class FireRateMod :
        TriggerMod,
        IWeaponProperty<WeaponFireRateContext>
    {
        public readonly static string Name = typeof(FireRateMod).Name;

        private readonly Queue<float> _expireTimes = new();

        public override void Reset()
        {
            _expireTimes.Clear();
        }

        public override void AddStack(WeaponTriggerContext context)
        {
            if (StackType == StackType.None) _expireTimes.Clear();
            _expireTimes.Enqueue(Clock.Time + Duration);
        }

        public void Invoke(WeaponFireRateContext context)
        {
            while (_expireTimes.TryPeek(out float time) && time < Clock.Time) _expireTimes.Dequeue();
            context.FireRate *= CalculateMod(_expireTimes.Count);
        }

        public override IWeaponProperty Clone()
        {
            FireRateMod copy = new();
            copy.CopyFrom(this);
            return copy;
        }

        public override void WriteName(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteString(nameof(Name), Name);
        }
    }
}
