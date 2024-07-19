using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class HealthMod :
        Effect
    {
        public float HealthChangeRel { get; set; } = 0f;
        public float CapRel { get; set; } = -1f;
        public float Cooldown { get; set; } = 0f;

        public override void TriggerReset() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            float cap = CapRel >= 0f ? CapRel : Math.Sign(HealthChangeRel);
            PlayerAgent owner = contexts[0].context.Weapon.Owner;
            float heal = HealthChangeRel * owner.Damage.HealthMax;
            heal *= contexts.Sum(context => context.triggerAmt);

            HealManager.DoHeal(
                owner,
                heal,
                cap * owner.Damage.HealthMax
                );
        }

        public override IWeaponProperty Clone()
        {
            HealthMod copy = new()
            {
                HealthChangeRel = HealthChangeRel,
                CapRel = CapRel,
                Cooldown = Cooldown,
                Trigger = Trigger?.Clone()
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HealthChangeRel), HealthChangeRel);
            writer.WriteNumber(nameof(CapRel), CapRel);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            SerializeTrigger(writer, options);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            base.DeserializeProperty(property, ref reader, options);
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
                default:
                    break;
            }
        }
    }
}
