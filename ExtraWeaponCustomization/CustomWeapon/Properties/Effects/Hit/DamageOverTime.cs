﻿using Agents;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Hit.DOT;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.JSON;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DamageOverTime :
        Effect,
        IGunProperty,
        IMeleeProperty,
        ITriggerCallbackAgentSync
    {
        public ushort SyncID { get; set; }
        public PlayerAgent Owner => CWC.Weapon.Owner;

        public float TotalDamage { get; private set; } = 0f;
        public float PrecisionDamageMulti { get; private set; } = 0f;
        public float FriendlyDamageMulti { get; private set; } = 1f;
        public float StaggerDamageMulti { get; private set; } = 0f;
        public float Duration { get; private set; } = 0f;
        public uint StackLimit { get; private set; } = 0;
        public bool IgnoreFalloff { get; private set; } = false;
        public bool DamageLimb { get; private set; } = true;
        public bool IgnoreArmor { get; private set; } = false;
        public bool IgnoreBackstab { get; private set; } = false;
        public bool IgnoreDamageMods { get; private set; } = false;
        private float _tickRate = 2f;
        public float TickRate
        {
            get { return _tickRate; }
            private set { _tickRate = Math.Max(0.01f, value); }
        }
        public bool ApplyAttackCooldown { get; private set; } = false;
        public Color GlowColor { get; private set; } = Color.black;
        public float GlowIntensity { get; private set; } = 1f;
        public float GlowRange { get; private set; } = 0f;
        public bool BatchStacks { get; private set; } = true;

        private readonly DOTController _controller = new();
        private readonly Dictionary<BaseDamageableWrapper, Queue<DOTInstance>> _lastDOTs = new();
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public DamageOverTime()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(DamageType.DOT, TriggerName.PreHit, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge);
        }

        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            foreach (TriggerContext tContext in triggerList)
            {
                var hitContext = (WeaponHitDamageableContext)tContext.context;
                AddDOT(hitContext, tContext.triggerAmt);
                Agent? agent = hitContext.Damageable.GetBaseAgent();
                if (agent != null)
                {
                    TriggerApplySync(agent, tContext.triggerAmt);
                    TriggerManager.SendInstance(this, agent, tContext.triggerAmt);
                }
            }
        }

        public override void TriggerReset()
        {
            _controller.Clear();
            TriggerResetSync();
        }

        public void TriggerApplySync(Agent target, float mod)
        {
            IDamageable? damBase = target.Type switch
            {
                AgentType.Player => target.Cast<PlayerAgent>().Damage.Cast<IDamageable>(),
                AgentType.Enemy => target.Cast<EnemyAgent>().Damage.Cast<IDamageable>(),
                _ => null
            };
            if (damBase == null) return;

            DOTGlowPooling.TryDoEffect(this, damBase, target.transform, mod);
        }

        public void TriggerResetSync()
        {
            DOTGlowPooling.TryEndEffect(this);
        }

        private void AddDOT(WeaponHitDamageableContext context, float triggerAmt)
        {
            TempWrapper.Set(context.Damageable);

            float falloff = IgnoreFalloff ? 1f : context.Falloff;
            float damage = TotalDamage * triggerAmt;
            float backstabMulti = 1f;
            if (!IgnoreBackstab)
                backstabMulti = context.Backstab;

            float precisionMulti = PrecisionDamageMulti;

            WeaponDamageContext damageContext = new(damage, precisionMulti, context.Damageable);
            CWC.Invoke(damageContext);
            if (!IgnoreDamageMods)
                damage = damageContext.Damage.Value;
            precisionMulti = damageContext.Precision.Value;

            damage *= EXPAPIWrapper.GetDamageMod(CWC.IsGun);

            if (StackLimit == 0)
                _controller.AddDOT(damage, falloff, precisionMulti, damageContext.BypassTumorCap, backstabMulti, context.Damageable, this);
            else
            {
                ClearDeadQueues();
                float nextTickTime = -1f;
                Queue<DOTInstance> queue;
                if (_lastDOTs.ContainsKey(TempWrapper))
                {
                    queue = _lastDOTs[TempWrapper];
                    if (queue.Count >= StackLimit)
                    {
                        // If the first DOT hasn't even done damage, no point in adding a new one
                        DOTInstance firstDot = queue.Peek();
                        if (!firstDot.Started)
                            return;

                        nextTickTime = firstDot.NextTickTime;
                        queue.Dequeue().Destroy();
                    }
                }
                else
                    _lastDOTs.Add(new BaseDamageableWrapper(context.Damageable), queue = new Queue<DOTInstance>());

                DOTInstance? newDot = _controller.AddDOT(damage, falloff, precisionMulti, damageContext.BypassTumorCap, backstabMulti, context.Damageable, this);
                if (newDot != null)
                {
                    queue.Enqueue(newDot);
                    if (nextTickTime >= 0f)
                        newDot.StartWithTargetTime(nextTickTime);
                }
            }
        }

        private void ClearDeadQueues()
        {
            List<BaseDamageableWrapper> wrappers = _lastDOTs.Keys.ToList();
            foreach (var wrapper in wrappers)
            {
                if (!wrapper.Alive)
                {
                    _lastDOTs.Remove(wrapper);
                    continue;
                }

                Queue<DOTInstance> queue = _lastDOTs[wrapper];
                while (queue.TryPeek(out var instance) && instance.Expired)
                    queue.Dequeue();

                if (queue.Count == 0)
                    _lastDOTs.Remove(wrapper);
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TotalDamage), TotalDamage);
            writer.WriteNumber(nameof(PrecisionDamageMulti), PrecisionDamageMulti);
            writer.WriteNumber(nameof(StaggerDamageMulti), StaggerDamageMulti);
            writer.WriteNumber(nameof(FriendlyDamageMulti), FriendlyDamageMulti);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteNumber(nameof(TickRate), TickRate);
            writer.WriteNumber(nameof(StackLimit), StackLimit);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreDamageMods), IgnoreDamageMods);
            writer.WriteBoolean(nameof(ApplyAttackCooldown), ApplyAttackCooldown);
            writer.WritePropertyName(nameof(GlowColor));
            EWCJson.Serialize(writer, GlowColor);
            writer.WriteNumber(nameof(GlowIntensity), GlowIntensity);
            writer.WriteNumber(nameof(GlowRange), GlowRange);
            SerializeTrigger(writer);
            writer.WriteBoolean(nameof(BatchStacks), BatchStacks);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "totaldamage":
                case "damage":
                    TotalDamage = reader.GetSingle();
                    break;
                case "precisiondamagemulti":
                case "precisionmulti":
                case "precisionmult":
                case "precision":
                    PrecisionDamageMulti = reader.GetSingle();
                    break;
                case "staggerdamagemulti":
                case "staggermulti":
                case "staggermult":
                case "stagger":
                    StaggerDamageMulti = reader.GetSingle();
                    break;
                case "friendlydamagemulti":
                case "friendlymulti":
                case "friendlymult":
                    FriendlyDamageMulti = reader.GetSingle();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "tickrate":
                case "hitrate":
                    TickRate = reader.GetSingle();
                    break;
                case "stacklimit":
                case "limit":
                    StackLimit = reader.GetUInt32();
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
                case "applyattackcooldowns":
                case "applyattackcooldown":
                    ApplyAttackCooldown = reader.GetBoolean();
                    break;
                case "glowcolor":
                    GlowColor = EWCJson.Deserialize<Color>(ref reader);
                    break;
                case "glowintensity":
                    GlowIntensity = reader.GetSingle();
                    break;
                case "glowrange":
                    GlowRange = reader.GetSingle();
                    break;
                case "batchstacks":
                    BatchStacks = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
