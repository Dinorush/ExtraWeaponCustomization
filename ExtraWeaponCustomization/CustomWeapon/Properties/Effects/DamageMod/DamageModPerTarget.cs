using ExtraWeaponCustomization.CustomWeapon.ObjectWrappers;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Agents;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        TriggerMod,
        IWeaponProperty<WeaponDamageContext>
    {
        private readonly Dictionary<AgentWrapper, Queue<TriggerInstance>> _expireTimes = new();
        private static AgentWrapper TempWrapper => AgentWrapper.SharedInstance;

        public DamageModPerTarget()
        {
            Trigger ??= new(ITrigger.GetTrigger(ITrigger.Hit)!);
            SetValidTriggers(ITrigger.Hit, ITrigger.Damage);
        }

        public override void TriggerReset()
        {
            _expireTimes.Clear();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (contexts.Count > 5)
            {
                Dictionary<AgentWrapper, float> triggerDict = new();
                foreach (var context in contexts)
                {
                    TempWrapper.SetAgent(((WeaponPreHitEnemyContext)context.context).Damageable.GetBaseAgent());
                    if (!triggerDict.ContainsKey(TempWrapper))
                        triggerDict.Add(new AgentWrapper(TempWrapper), 0);
                    triggerDict[TempWrapper] += context.triggerAmt;
                }

                foreach ((AgentWrapper wrapper, float triggerAmt) in triggerDict)
                    AddTriggerInstance(wrapper, triggerAmt);
            }
            else
            {
                foreach (var context in contexts)
                    AddTriggerInstance(
                        new AgentWrapper(((WeaponPreHitEnemyContext)context.context).Damageable.GetBaseAgent()),
                        context.triggerAmt
                        );
            }
        }

        private void AddTriggerInstance(AgentWrapper wrapper, float triggerAmt)
        {
            float mod = CalculateMod(triggerAmt);
            if (!_expireTimes.ContainsKey(wrapper))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _expireTimes.Keys
                    .Where(wrapper => wrapper.Agent == null || !wrapper.Agent.Alive)
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

            TempWrapper.SetAgent(agent);
            if (!_expireTimes.TryGetValue(TempWrapper, out Queue<TriggerInstance>? queue)) return;

            while (queue.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) queue.Dequeue();

            context.Damage.AddMod(CalculateMod(queue), StackLayer);
        }

        public override IWeaponProperty Clone()
        {
            DamageModPerTarget copy = new();
            copy.CopyFrom(this);
            return copy;
        }

        public override void WriteName(Utf8JsonWriter writer)
        {
            writer.WriteString("Name", GetType().Name);
        }
    }
}
