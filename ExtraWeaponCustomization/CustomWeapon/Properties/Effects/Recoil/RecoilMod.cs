﻿using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class RecoilMod :
        TriggerMod,
        IGunProperty,
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

            if (_expireTimes.Count > 0)
                context.AddMod(CalculateMod(_expireTimes), StackLayer);
        }

        protected override void WriteName(Utf8JsonWriter writer)
        {
            writer.WriteString("Name", GetType().Name);
        }
    }
}
