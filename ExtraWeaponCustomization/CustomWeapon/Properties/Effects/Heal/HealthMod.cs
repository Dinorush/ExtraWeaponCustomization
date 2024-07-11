using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Player;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class HealthMod :
        Effect,
        IWeaponProperty<WeaponTriggerContext>
    {
        public float HealthChangeRel { get; set; } = 0f;
        public float CapRel { get; set; } = -1f;
        public float Cooldown { get; set; } = 0f;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;
        private float _lastTriggerTime = 0f;

        public void Invoke(WeaponTriggerContext context)
        {
            if (!context.Type.IsType(TriggerType) || Clock.Time < _lastTriggerTime + Cooldown) return;

            float cap = CapRel >= 0f ? CapRel : Math.Sign(HealthChangeRel);
            PlayerAgent owner = context.Weapon.Owner;
            float heal = HealthChangeRel * owner.Damage.HealthMax;
            if (context.Type.IsType(TriggerType.OnDamage))
                heal *= ((WeaponOnDamageContext) context).Damage;

            HealManager.DoHeal(
                owner,
                heal,
                cap * owner.Damage.HealthMax
                );

            _lastTriggerTime = Clock.Time;
        }

        public IWeaponProperty Clone()
        {
            HealthMod copy = new()
            {
                HealthChangeRel = HealthChangeRel,
                CapRel = CapRel,
                Cooldown = Cooldown,
                TriggerType = TriggerType
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HealthChangeRel), HealthChangeRel);
            writer.WriteNumber(nameof(CapRel), CapRel);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "healthchangerel":
                case "healthchange":
                case "healthrel":
                case "health":
                case "healrel":
                case "heal":
                    HealthChangeRel = reader.GetSingle();
                    break;
                case "caprel":
                case "cap":
                    CapRel = reader.GetSingle();
                    break;
                case "cooldown":
                    Cooldown = reader.GetSingle();
                    break;
                case "triggertype":
                case "trigger":
                    TriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    break;
                default:
                    break;
            }
        }
    }
}
