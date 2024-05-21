using Agents;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Il2CppSystem.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        TriggerMod,
        IWeaponProperty<WeaponDamageContext>
    {
        public readonly static string Name = typeof(DamageModPerTarget).Name;

        private readonly Dictionary<Agent, Queue<float>> _expireTimes = new();

        public override void Reset()
        {
            _expireTimes.Clear();
        }

        public override void AddStack(WeaponTriggerContext context)
        {
            // TriggerType is enforced to be OnHit
            WeaponPreHitEnemyContext damageContext = (WeaponPreHitEnemyContext) context;
            Agent agent = damageContext.Damageable!.GetBaseAgent();

            if (!_expireTimes.ContainsKey(agent))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                List<Agent> dead = new();
                foreach (var key in _expireTimes.Keys) if (key.GetInstanceID() == 0 || !key.Alive) dead.Add(key!);
                foreach (var key in dead) _expireTimes.Remove(key);

                _expireTimes[agent] = new Queue<float>();
            }

            if (StackType == StackType.None) _expireTimes[agent].Clear();

            _expireTimes[agent].Enqueue(Clock.Time + Duration);
        }

        public void Invoke(WeaponDamageContext context)
        {
            Agent agent = context.Damageable!.GetBaseAgent();
            if (!_expireTimes.ContainsKey(agent)) return;

            while (_expireTimes[agent].Count > 0 && _expireTimes[agent].Peek() < Clock.Time) _expireTimes[agent].Dequeue();
            context.Damage *= CalculateMod(_expireTimes[agent].Count);
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
            if (!TriggerType.IsType(TriggerType.OnHit)) TriggerType = TriggerType.Invalid;
        }
    }
}
