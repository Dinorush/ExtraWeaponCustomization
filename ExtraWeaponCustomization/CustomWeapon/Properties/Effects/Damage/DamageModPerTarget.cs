using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponDamageContext>
    {
        private readonly Dictionary<BaseDamageableWrapper, Queue<TriggerInstance>> _expireTimes = new();
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public DamageModPerTarget()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(TriggerName.PreHit, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge);
        }

        public override void TriggerReset()
        {
            _expireTimes.Clear();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (contexts.Count > 5)
            {
                Dictionary<BaseDamageableWrapper, float> triggerDict = new();
                foreach (var context in contexts)
                {
                    IDamageable damageable = ((WeaponHitDamageableContext)context.context).Damageable;
                    if (damageable == null) continue;

                    TempWrapper.Set(damageable);
                    if (!triggerDict.ContainsKey(TempWrapper))
                        triggerDict.Add(new BaseDamageableWrapper(TempWrapper), 0);
                    triggerDict[TempWrapper] += context.triggerAmt;
                }

                foreach ((BaseDamageableWrapper wrapper, float triggerAmt) in triggerDict)
                    AddTriggerInstance(wrapper, triggerAmt);
            }
            else
            {
                foreach (var context in contexts)
                {
                    IDamageable damageable = ((WeaponHitDamageableContext)context.context).Damageable;
                    if (damageable == null) continue;

                    AddTriggerInstance(
                        new BaseDamageableWrapper(damageable),
                        context.triggerAmt
                        );
                }
            }
        }

        private void AddTriggerInstance(BaseDamageableWrapper wrapper, float triggerAmt)
        {
            float mod = CalculateMod(triggerAmt);
            if (!_expireTimes.ContainsKey(wrapper))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _expireTimes.Keys
                    .Where(wrapper => !wrapper.Alive)
                    .ToList()
                    .ForEach(wrapper => _expireTimes.Remove(wrapper));

                _expireTimes[wrapper] = new Queue<TriggerInstance>();
            }

            if (StackType == StackType.None) _expireTimes[wrapper].Clear();

            _expireTimes[wrapper].Enqueue(new TriggerInstance(mod, Clock.Time + Duration));
            RefreshPreviousInstances(_expireTimes[wrapper]);
        }

        public void Invoke(WeaponDamageContext context)
        {
            TempWrapper.Set(context.Damageable);
            if (!_expireTimes.TryGetValue(TempWrapper, out Queue<TriggerInstance>? queue)) return;

            while (queue.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) queue.Dequeue();

            if (queue.Count > 0)
                context.Damage.AddMod(CalculateMod(queue), StackLayer);
        }

        protected override void WriteName(Utf8JsonWriter writer)
        {
            writer.WriteString("Name", GetType().Name);
        }
    }
}
