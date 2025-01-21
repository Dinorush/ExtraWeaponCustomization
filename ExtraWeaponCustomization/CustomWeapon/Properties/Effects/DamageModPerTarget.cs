using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponDamageContext>
    {
        private readonly Dictionary<BaseDamageableWrapper, TriggerStack> _triggerStacks = new();
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public DamageModPerTarget()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(TriggerName.PreHit, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge);
        }

        public override void TriggerReset()
        {
            _triggerStacks.Clear();
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
            if (!_triggerStacks.ContainsKey(wrapper))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _triggerStacks.Keys
                    .Where(wrapper => !wrapper.Alive)
                    .ToList()
                    .ForEach(wrapper => _triggerStacks.Remove(wrapper));

                _triggerStacks[wrapper] = new TriggerStack(this);
            }

            if (StackType == StackType.None) _triggerStacks[wrapper].Clear();

            _triggerStacks[wrapper].Add(triggerAmt);
        }

        public void Invoke(WeaponDamageContext context)
        {
            TempWrapper.Set(context.Damageable);
            if (!_triggerStacks.TryGetValue(TempWrapper, out TriggerStack? triggerStack)) return;

            if (triggerStack.TryGetMod(out float mod))
                context.Damage.AddMod(mod, StackLayer);
        }
    }
}
