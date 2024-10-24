using EWC.CustomWeapon.Properties.Effects.Heal;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class HealthMod :
        Effect,
        IGunProperty,
        IMeleeProperty
    {
        public float HealthChangeRel { get; private set; } = 0f;
        public float CapRel { get; private set; } = -1f;

        public override void TriggerReset() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            float cap = CapRel >= 0f ? CapRel : Math.Sign(HealthChangeRel);
            PlayerAgent owner = CWC.Weapon.Owner;
            float heal = HealthChangeRel * owner.Damage.HealthMax;
            heal *= contexts.Sum(context => context.triggerAmt);

            HealManager.DoHeal(
                owner,
                heal,
                cap * owner.Damage.HealthMax
                );
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HealthChangeRel), HealthChangeRel);
            writer.WriteNumber(nameof(CapRel), CapRel);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
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
                default:
                    break;
            }
        }
    }
}
