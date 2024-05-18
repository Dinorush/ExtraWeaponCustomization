using Agents;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Il2CppSystem.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DamageModPerTarget : 
        IWeaponProperty<WeaponTriggerContext>,
        IWeaponProperty<WeaponDamageContext>
    {
        public readonly static string Name = typeof(DamageModPerTarget).Name;
        public bool AllowStack { get; } = true;

        public float Mod { get; set; } = 1f;
        public float Duration { get; set; } = 0f;
        public StackType StackType { get; set; } = StackType.None;
        public TriggerType TriggerType { get; set; } = TriggerType.OnHit;
        public TriggerType ResetTriggerType { get; set; } = TriggerType.Invalid;

        private readonly Dictionary<Agent, Queue<float>> _expireTimes = new();

        public void Invoke(WeaponTriggerContext context)
        {
            if (context.Type.IsType(ResetTriggerType))
            {
                _expireTimes.Clear();
                return;
            }
            else if (!context.Type.IsType(TriggerType)) return;

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
            context.Damage *= StackType.CalculateMod(Mod, _expireTimes[agent].Count);
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "mod":
                    Mod = reader.GetSingle();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "stacktype":
                case "stack":
                    StackType = reader.GetString()?.ToStackType() ?? StackType.Invalid;
                    break;
                case "triggertype":
                case "trigger":
                    TriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    if (!TriggerType.IsType(TriggerType.OnHit)) TriggerType = TriggerType.Invalid;
                    break;
                case "resettriggertype":
                case "resettrigger":
                    ResetTriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    break;
                default:
                    break;
            }
        }
    }
}
