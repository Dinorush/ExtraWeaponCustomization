using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class RecoilMod :
        TriggerMod,
        IWeaponProperty<WeaponRecoilContext>
    {
        private readonly Queue<TriggerInstance> _expireTimes = new();

        public override void TriggerReset()
        {
            _expireTimes.Clear();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (StackType == StackType.None)
                _expireTimes.Clear();

            _expireTimes.Enqueue(new TriggerInstance(ConvertTriggersToMod(contexts), Clock.Time + Duration));
        }

        public void Invoke(WeaponRecoilContext context)
        {
            while (_expireTimes.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) _expireTimes.Dequeue();

            context.AddMod(CalculateMod(_expireTimes), StackLayer);
        }

        public override IWeaponProperty Clone()
        {
            RecoilMod copy = new();
            copy.CopyFrom(this);
            return copy;
        }

        public override void WriteName(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteString("Name", GetType().Name);
        }
    }
}
