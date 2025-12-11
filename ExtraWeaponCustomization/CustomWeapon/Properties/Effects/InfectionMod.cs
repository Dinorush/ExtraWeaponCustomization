using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Infection;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class InfectionMod :
        Effect
    {
        public float InfectionChangeRel { get; private set; } = 0f;
        public float CapRel { get; private set; } = -1f;
        public bool ApplyToTarget { get; private set; } = false;
        public bool ApplyToBots { get; private set; } = true;
        public bool EnableFX { get; private set; } = true;

        public override void TriggerReset() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (ApplyToTarget)
            {
                foreach (var tContext in contexts)
                {
                    PlayerAgent? target;
                    if (tContext.context is WeaponHitDamageableContextBase damContext && damContext.DamageType.HasFlag(DamageType.Player))
                    {
                        target = damContext.Damageable.GetBaseAgent().Cast<PlayerAgent>();
                        DoInfection(target, tContext.triggerAmt, damContext.Position, damContext.Direction);
                    }
                    else
                    {
                        target = CWC.Owner.Player;
                        if (target != null)
                            DoInfection(target, tContext.triggerAmt);
                    }
                }
            }
            else
            {
                if (CWC.Owner.Player != null)
                    DoInfection(CWC.Owner.Player, contexts.Sum(tContext => tContext.triggerAmt));
            }
        }

        private void DoInfection(PlayerAgent target, float mod, Vector3 pos, Vector3 dir)
        {
            if (!ApplyToBots && target.Owner.IsBot) return;

            float infect = CalcInfection(target, mod);
            if (infect == 0f) return;

            pInfection data = new()
            {
                amount = infect,
                mode = pInfectionMode.Add
            };

            if (EnableFX && !target.Owner.IsBot)
            {
                if (infect < 0)
                    data.effect = pInfectionEffect.DisinfectionPack;
                else
                    InfectionManager.DoInfectFX(target, infect, pos, dir);
            }

            target.Damage.ModifyInfection(data, true, true);
        }


        private void DoInfection(PlayerAgent target, float mod)
        {
            float infect = CalcInfection(target, mod);
            if (infect == 0f) return;

            pInfection data = new()
            {
                amount = infect,
                mode = pInfectionMode.Add
            };

            if (EnableFX)
            {
                if (infect < 0)
                    data.effect = pInfectionEffect.DisinfectionPack;
                else
                    InfectionManager.DoDirectInfectFX(target, infect);
            }

            target.Damage.ModifyInfection(data, true, true);
        }

        private float CalcInfection(PlayerAgent target, float mod)
        {
            float cap = CapRel >= 0f ? CapRel : Math.Sign(InfectionChangeRel);
            float infection = InfectionChangeRel * mod;

            var damBase = target.Damage;
            if (infection > 0)
            {
                if (damBase.Infection >= cap) return 0f;
                infection = Math.Min(infection, cap - damBase.Infection);
            }
            else
            {
                if (damBase.Infection <= cap) return 0f;
                infection = Math.Max(infection, cap - damBase.Infection);
            }

            return infection;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(InfectionChangeRel), InfectionChangeRel);
            writer.WriteNumber(nameof(CapRel), CapRel);
            writer.WriteBoolean(nameof(ApplyToTarget), ApplyToTarget);
            writer.WriteBoolean(nameof(EnableFX), EnableFX);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "infectionchangerel":
                case "infectionchange":
                case "infectionrel":
                case "infection":
                case "infectrel":
                case "infect":
                    InfectionChangeRel = reader.GetSingle();
                    break;
                case "caprel":
                case "cap":
                    CapRel = reader.GetSingle();
                    break;
                case "applytotarget":
                    ApplyToTarget = reader.GetBoolean();
                    break;
                case "applytobots":
                    ApplyToBots = reader.GetBoolean();
                    break;
                case "enablefx":
                    EnableFX = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
