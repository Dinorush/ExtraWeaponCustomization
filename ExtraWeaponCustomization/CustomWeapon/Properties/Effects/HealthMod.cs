using EWC.CustomWeapon.Properties.Effects.Heal;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Dependencies;
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
        public bool CancelRegen { get; private set; } = false;
        public bool StopBleed { get; private set; } = false;
        public bool ApplyToTarget { get; private set; } = false;

        public override void TriggerReset() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (ApplyToTarget)
            {
                foreach (var tContext in contexts)
                {
                    PlayerAgent target;
                    if (tContext.context is WeaponHitDamageableContextBase damContext && damContext.DamageType.HasFlag(Enums.DamageType.Player))
                        target = damContext.Damageable.GetBaseAgent().Cast<PlayerAgent>();
                    else
                        target = CWC.Weapon.Owner;

                    DoHeal(target, tContext.triggerAmt);
                }
            }
            else
            {
                DoHeal(CWC.Weapon.Owner, contexts.Sum(tContext => tContext.triggerAmt));
            }
        }

        private void DoHeal(PlayerAgent target, float mod)
        {
            float cap = CapRel >= 0f ? CapRel : Math.Sign(HealthChangeRel);
            float heal = HealthChangeRel * target.Damage.HealthMax * mod;

            HealManager.DoHeal(
                target,
                heal,
                cap * target.Damage.HealthMax,
                this
                );

            if (StopBleed)
                EECAPIWrapper.StopBleed(target);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HealthChangeRel), HealthChangeRel);
            writer.WriteNumber(nameof(CapRel), CapRel);
            writer.WriteBoolean(nameof(CancelRegen), CancelRegen);
            writer.WriteBoolean(nameof(StopBleed), StopBleed);
            writer.WriteBoolean(nameof(ApplyToTarget), ApplyToTarget);
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
                case "cancelregen":
                    CancelRegen = reader.GetBoolean();
                    break;
                case "stopbleed":
                    StopBleed = reader.GetBoolean();
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
