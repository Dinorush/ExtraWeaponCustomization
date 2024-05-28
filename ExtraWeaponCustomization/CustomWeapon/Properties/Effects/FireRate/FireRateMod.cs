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

        private readonly Queue<TriggerInstance> _expireTimes = new();

        public override void Reset()
        {
            _expireTimes.Clear();
        }

        public override void AddStack(WeaponTriggerContext context)
        {
            if (StackType == StackType.None) _expireTimes.Clear();

            float mod = Mod;
            if (context.Type.IsType(TriggerType.OnDamage))
                mod = CalculateOnDamageMod(((WeaponOnDamageContext) context).Damage);
            _expireTimes.Enqueue(new TriggerInstance(mod, Clock.Time + Duration));
        }

        public void Invoke(WeaponFireRateContext context)
        {
            while (_expireTimes.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) _expireTimes.Dequeue();
            context.AddMod(CalculateMod(_expireTimes), StackLayer);
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
