using ExtraWeaponCustomization.CustomWeapon.ObjectWrappers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        TriggerMod,
        IWeaponProperty<WeaponDamageContext>
    {
        public readonly static string Name = typeof(DamageModPerTarget).Name;

        private readonly Dictionary<AgentWrapper, Queue<TriggerInstance>> _expireTimes = new();
        private static AgentWrapper TempWrapper => AgentWrapper.SharedInstance;

        public override void Reset()
        {
            _expireTimes.Clear();
        }

        public override void AddStack(WeaponTriggerContext context)
        {
            float mod = Mod;
            // Enforced to be one of these two types
            if (context.Type.IsType(TriggerType.OnHit))
                TempWrapper.SetAgent(((WeaponPreHitEnemyContext) context).Damageable.GetBaseAgent());
            else if (context.Type.IsType(TriggerType.OnDamage))
            {
                WeaponOnDamageContext damageContext = (WeaponOnDamageContext) context;
                TempWrapper.SetAgent(damageContext.Damageable.GetBaseAgent());
                mod = CalculateOnDamageMod(damageContext.Damage);
            }

            if (!_expireTimes.ContainsKey(TempWrapper))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _expireTimes.Keys
                    .Where(wrapper => wrapper.Agent == null || !wrapper.Agent.Alive)
                    .ToList()
                    .ForEach(wrapper => _expireTimes.Remove(wrapper));

                _expireTimes[new AgentWrapper(TempWrapper)] = new Queue<TriggerInstance>();
            }

            if (StackType == StackType.None) _expireTimes[TempWrapper].Clear();

            _expireTimes[TempWrapper].Enqueue(new TriggerInstance(mod, Clock.Time + Duration));
        }

        public void Invoke(WeaponDamageContext context)
        {
            TempWrapper.SetAgent(context.Damageable!.GetBaseAgent());
            if (!_expireTimes.ContainsKey(TempWrapper)) return;

            Queue<TriggerInstance> queue = _expireTimes[TempWrapper];
            while (queue.TryPeek(out TriggerInstance ti) && ti.endTime < Clock.Time) queue.Dequeue();
            context.AddMod(CalculateMod(queue), StackLayer);
        }

        public override IWeaponProperty Clone()
        {
            DamageModPerTarget copy = new();
            copy.CopyFrom(this);
            return copy;
        }

        public override void WriteName(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteString(nameof(Name), Name);
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            if (!TriggerType.IsType(TriggerType.OnHit) && !TriggerType.IsType(TriggerType.OnDamage))
                TriggerType = TriggerType.Invalid;
        }
    }
}
