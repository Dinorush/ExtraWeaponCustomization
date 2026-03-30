using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Stamina;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using Player;
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
        public bool ApplyToTarget { get; private set; } = false;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;

        public override bool ValidProperty()
        {
            if (!ApplyToTarget && !CWC.Owner.IsType(OwnerType.Local))
                return false;
            return base.ValidProperty();
        }

        public override void TriggerReset() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            float cap = Cap >= 0f ? Cap : (StaminaChange > 0 ? 1 : 0);
            if (ApplyToTarget)
            {
                foreach (var tContext in contexts)
                {
                    PlayerAgent? target;
                    if (tContext.context is WeaponHitDamageableContextBase damContext && damContext.DamageType.HasFlag(DamageType.Player))
                        target = damContext.Damageable.GetBaseAgent().Cast<PlayerAgent>();
                    else
                        target = CWC.Owner.Player;

                    if (target != null)
                        StaminaManager.DoStaminaChange(target, StaminaChange * tContext.triggerAmt, cap, this);
                }
            }
            else
            {
                if (CWC.Owner.Player != null)
                    StaminaManager.DoStaminaChange(CWC.Owner.Player, StaminaChange * contexts.Sum(tContext => tContext.triggerAmt), cap, this);
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(StaminaChange), StaminaChange);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteBoolean(nameof(CancelRegen), CancelRegen);
            writer.WriteBoolean(nameof(ApplyToTarget), ApplyToTarget);
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
                case "applytotarget":
                    ApplyToTarget = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
