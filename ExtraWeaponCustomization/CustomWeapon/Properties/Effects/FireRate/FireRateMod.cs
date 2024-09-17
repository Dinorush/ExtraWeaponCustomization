using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.FireRate;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Player;
using System.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public class FireRateMod :
        TriggerMod,
        IWeaponProperty<WeaponFireRateContext>,
        IWeaponProperty<WeaponFireRateModContextSync>
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

            float mod = ConvertTriggersToMod(contexts);
            _expireTimes.Enqueue(new TriggerInstance(mod, Clock.Time + Duration));

            FireRateModManager.SendInstance(CWC.Weapon.Owner.Owner, PlayerAmmoStorage.GetSlotFromAmmoType(CWC.Weapon.AmmoType), mod);
        }

        public void Invoke(WeaponFireRateContext context)
        {
            while (_expireTimes.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) _expireTimes.Dequeue();

            context.AddMod(CalculateMod(_expireTimes), StackLayer);
        }

        public void Invoke(WeaponFireRateModContextSync context)
        {
            if (StackType == StackType.None)
                _expireTimes.Clear();

            _expireTimes.Enqueue(new TriggerInstance(context.Mod, Clock.Time + Duration));
        }

        public override IWeaponProperty Clone()
        {
            FireRateMod copy = new();
            copy.CopyFrom(this);
            return copy;
        }

        public override void WriteName(Utf8JsonWriter writer)
        {
            writer.WriteString("Name", GetType().Name);
        }
    }
}
