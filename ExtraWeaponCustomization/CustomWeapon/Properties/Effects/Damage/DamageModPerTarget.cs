using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Agents;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponDamageContext>
    {
        private readonly Dictionary<ObjectWrapper<Agent>, Queue<TriggerInstance>> _expireTimes = new();
        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;

        public DamageModPerTarget()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(TriggerName.Hit, TriggerName.Damage, TriggerName.Charge);
        }

        public override void TriggerReset()
        {
            _expireTimes.Clear();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (contexts.Count > 5)
            {
                Dictionary<ObjectWrapper<Agent>, float> triggerDict = new();
                foreach (var context in contexts)
                {
                    IDamageable damageable = ((WeaponPreHitEnemyContext)context.context).Damageable;
                    if (damageable == null) continue;

                    TempWrapper.SetObject(damageable.GetBaseAgent());
                    if (!triggerDict.ContainsKey(TempWrapper))
                        triggerDict.Add(new ObjectWrapper<Agent>(TempWrapper), 0);
                    triggerDict[TempWrapper] += context.triggerAmt;
                }

                foreach ((ObjectWrapper<Agent> wrapper, float triggerAmt) in triggerDict)
                    AddTriggerInstance(wrapper, triggerAmt);
            }
            else
            {
                foreach (var context in contexts)
                {
                    IDamageable damageable = ((WeaponPreHitEnemyContext)context.context).Damageable;
                    if (damageable == null) continue;

                    AddTriggerInstance(
                        new ObjectWrapper<Agent>(damageable.GetBaseAgent()),
                        context.triggerAmt
                        );
                }
            }
        }

        private void AddTriggerInstance(ObjectWrapper<Agent> wrapper, float triggerAmt)
        {
            float mod = CalculateMod(triggerAmt);
            if (!_expireTimes.ContainsKey(wrapper))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _expireTimes.Keys
                    .Where(wrapper => wrapper.Object == null || !wrapper.Object.Alive)
                    .ToList()
                    .ForEach(wrapper => _expireTimes.Remove(wrapper));

                _expireTimes[wrapper] = new Queue<TriggerInstance>();
            }

            if (StackType == StackType.None) _expireTimes[wrapper].Clear();

            _expireTimes[wrapper].Enqueue(new TriggerInstance(mod, Clock.Time + Duration));
        }

        public void Invoke(WeaponDamageContext context)
        {
            Agent agent = context.Damageable.GetBaseAgent();
            if (agent == null) return;

            TempWrapper.SetObject(agent);
            if (!_expireTimes.TryGetValue(TempWrapper, out Queue<TriggerInstance>? queue)) return;

            while (queue.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) queue.Dequeue();

            context.Damage.AddMod(CalculateMod(queue), StackLayer);
        }

        public override void WriteName(Utf8JsonWriter writer)
        {
            writer.WriteString("Name", GetType().Name);
        }
    }
}
