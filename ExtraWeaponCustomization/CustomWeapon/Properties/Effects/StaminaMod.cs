using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class StaminaMod :
        Effect
    {
        public float StaminaChange { get; private set; } = 0f;
        public float Cap { get; private set; } = -1f;
        public bool CancelRegen { get; private set; } = false;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;

        public override void TriggerReset() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            float cap = Cap >= 0f ? Cap : (StaminaChange > 0 ? 1 : 0);
            float stamChange = StaminaChange * contexts.Sum(tContext => tContext.triggerAmt);

            var stam = CWC.Owner.Player!.Stamina;
            // Have to write this myself since vanilla UseStamina can't give stamina
            if (stamChange > 0)
            {
                if (DramaManager.InActionState)
                    cap = Math.Min(cap, stam.PlayerData.StaminaMaximumCapWhenInCombat);

                if (stam.Stamina >= cap) return;

                stam.Stamina = Math.Min(cap, stam.Stamina + stamChange);
            }
            else
            {
                if (!DramaManager.InActionState)
                    cap = Math.Max(cap, stam.PlayerData.StaminaMinimumCapWhenNotInCombat);

                if (stam.Stamina <= cap) return;

                stam.Stamina = Math.Max(cap, stam.Stamina + stamChange);
            }

            if (CancelRegen)
                stam.m_lastExertion = Clock.Time;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(StaminaChange), StaminaChange);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteBoolean(nameof(CancelRegen), CancelRegen);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "staminachangerel":
                case "staminachange":
                case "staminarel":
                case "stamina":
                    StaminaChange = reader.GetSingle();
                    break;
                case "caprel":
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "cancelregen":
                    CancelRegen = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
