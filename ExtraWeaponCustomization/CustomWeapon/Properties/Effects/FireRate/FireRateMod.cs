﻿using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public class FireRateMod :
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponFireRateContext>
    {
        public ushort SyncID { get; set; }
        private readonly Queue<TriggerInstance> _expireTimes = new();

        public override void TriggerReset()
        {
            _expireTimes.Clear();

            TriggerManager.SendReset(this);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (StackType == StackType.None)
                _expireTimes.Clear();

            float mod = ConvertTriggersToMod(contexts);
            _expireTimes.Enqueue(new TriggerInstance(mod, Clock.Time + Duration));

            TriggerManager.SendInstance(this, mod);
        }

        public void TriggerResetSync()
        {
            _expireTimes.Clear();
        }

        public void TriggerApplySync(float mod)
        {
            if (StackType == StackType.None)
                _expireTimes.Clear();
            _expireTimes.Enqueue(new TriggerInstance(mod, Clock.Time + Duration));
        }

        public void Invoke(WeaponFireRateContext context)
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
