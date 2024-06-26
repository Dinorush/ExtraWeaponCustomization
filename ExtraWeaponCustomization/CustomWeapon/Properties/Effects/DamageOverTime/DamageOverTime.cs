﻿using ExtraWeaponCustomization.CustomWeapon.ObjectWrappers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Dependencies;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DamageOverTime :
        IWeaponProperty<WeaponPreHitEnemyContext>
    {
        public bool AllowStack { get; } = true;
        public BulletWeapon? Weapon { get; set; }
        public PlayerAgent? Owner => Weapon?.Owner;

        public float TotalDamage { get; set; } = 0f;
        public float PrecisionMult { get; set; } = 0f;
        public float StaggerMult { get; set; } = 0f;
        public float Duration { get; set; } = 0f;
        public bool Stacks { get; set; } = true;
        public bool IgnoreFalloff { get; set; } = false;
        public bool DamageLimb { get; set; } = true;
        public bool IgnoreArmor { get; set; } = false;
        public bool IgnoreBackstab { get; set; } = false;
        public bool IgnoreDamageMods { get; set; } = false;
        private float _tickRate = 2f;
        public float TickRate
        {
            get { return _tickRate; }
            set { _tickRate = MathF.Max(0.01f, value); }
        }
        public TriggerType TriggerType { get; set; } = TriggerType.OnHit;

        private readonly DOTController _controller = new();
        private readonly Dictionary<AgentWrapper, DOTInstance> _lastDOTs = new();
        private static AgentWrapper TempWrapper => AgentWrapper.SharedInstance;

        public void Invoke(WeaponPreHitEnemyContext context)
        {
            if (!context.Type.IsType(TriggerType)) return;

            if (Weapon == null)
                Weapon = context.Weapon;

            Dam_EnemyDamageLimb? limb = context.Damageable.TryCast<Dam_EnemyDamageLimb>();
            if (limb == null || limb.m_armorDamageMulti == 0 || limb.m_base.IsImortal == true) return;
            float damage = TotalDamage * (IgnoreFalloff ? 1f : context.Falloff);
            float backstabMulti = IgnoreBackstab ? 1f : context.Backstab;

            EXPAPIWrapper.ApplyMod(ref damage);

            if (!IgnoreDamageMods)
            {
                WeaponDamageContext damageContext = new(damage, context.Damageable, context.Weapon);
                damageContext.Weapon.GetComponent<CustomWeaponComponent>().Invoke(damageContext);
                damage = damageContext.Value;
            }

            // If it doesn't stack, need to kill the existing DOT and add a new one
            if (!Stacks)
            {
                _lastDOTs.Keys
                .Where(wrapper => wrapper.Agent == null || !wrapper.Agent.Alive || _lastDOTs[wrapper].Expired)
                .ToList()
                .ForEach(wrapper => {
                    _lastDOTs.Remove(wrapper);
                });

                TempWrapper.SetAgent(limb.GetBaseAgent());

                if (_lastDOTs.ContainsKey(TempWrapper))
                {
                    DOTInstance? lastDot = _lastDOTs[TempWrapper];
                    if (!lastDot.Started)
                        return;

                    float nextTickTime = lastDot.NextTickTime;
                    lastDot.Destroy();
                    lastDot = _controller.AddDOT(damage, backstabMulti, context.Damageable, this);
                    if (lastDot != null)
                    {
                        lastDot.StartWithTargetTime(nextTickTime);
                        _lastDOTs[TempWrapper] = lastDot;
                    }
                    else
                        _lastDOTs.Remove(TempWrapper);
                }
                else
                {
                    DOTInstance? newDOT = _controller.AddDOT(damage, backstabMulti, context.Damageable, this);
                    if (newDOT != null)
                        _lastDOTs[new AgentWrapper(limb.GetBaseAgent())] = newDOT;
                }
            }
            else
                _controller.AddDOT(damage, backstabMulti, context.Damageable, this);
        }

        public IWeaponProperty Clone()
        {
            DamageOverTime copy = new()
            {
                TotalDamage = TotalDamage,
                PrecisionMult = PrecisionMult,
                StaggerMult = StaggerMult,
                Duration = Duration,
                Stacks = Stacks,
                IgnoreFalloff = IgnoreFalloff,
                DamageLimb = DamageLimb,
                IgnoreArmor = IgnoreArmor,
                IgnoreBackstab = IgnoreBackstab,
                IgnoreDamageMods = IgnoreDamageMods,
                TickRate = TickRate,
                TriggerType = TriggerType
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TotalDamage), TotalDamage);
            writer.WriteNumber(nameof(PrecisionMult), PrecisionMult);
            writer.WriteNumber(nameof(StaggerMult), StaggerMult);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteNumber(nameof(TickRate), TickRate);
            writer.WriteBoolean(nameof(Stacks), Stacks);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreDamageMods), IgnoreDamageMods);
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "totaldamage":
                case "damage":
                    TotalDamage = reader.GetSingle();
                    break;
                case "precisionmult":
                case "precision":
                    PrecisionMult = reader.GetSingle();
                    break;
                case "staggermult":
                case "stagger":
                    StaggerMult = reader.GetSingle();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "tickrate":
                case "hitrate":
                    TickRate = reader.GetSingle();
                    break;
                case "stacks":
                case "stack":
                    Stacks = reader.GetBoolean();
                    break;
                case "ignorefalloff":
                    IgnoreFalloff = reader.GetBoolean();
                    break;
                case "damagelimb":
                    DamageLimb = reader.GetBoolean();
                    break;
                case "ignorearmor":
                    IgnoreArmor = reader.GetBoolean();
                    break;
                case "ignorebackstab":
                case "ignorebackdamage":
                case "ignorebackbonus":
                    IgnoreBackstab = reader.GetBoolean();
                    break;
                case "ignoredamagemods":
                case "ignoredamagemod":
                    IgnoreDamageMods = reader.GetBoolean();
                    break;
                case "triggertype":
                case "trigger":
                    TriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    if (!TriggerType.IsType(TriggerType.OnHit)) TriggerType = TriggerType.Invalid;
                    break;
                default:
                    break;
            }
        }
    }
}
