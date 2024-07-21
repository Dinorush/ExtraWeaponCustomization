using ExtraWeaponCustomization.CustomWeapon.ObjectWrappers;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
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
        Effect
    {
        public BulletWeapon? Weapon { get; set; }
        public PlayerAgent? Owner => Weapon?.Owner;

        public float TotalDamage { get; set; } = 0f;
        public float PrecisionDamageMulti { get; set; } = 0f;
        public float StaggerDamageMulti { get; set; } = 0f;
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

        private readonly DOTController _controller = new();
        private readonly Dictionary<AgentWrapper, DOTInstance> _lastDOTs = new();
        private static AgentWrapper TempWrapper => AgentWrapper.SharedInstance;

        public DamageOverTime()
        {
            Trigger ??= new(ITrigger.GetTrigger(ITrigger.Hit)!);
            SetValidTriggers(DamageType.DOT, ITrigger.Hit, ITrigger.Damage);
        }

        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            if (Weapon == null)
                Weapon = triggerList[0].context.Weapon;

            foreach (TriggerContext tContext in triggerList)
                AddDOT((WeaponPreHitEnemyContext)tContext.context, tContext.triggerAmt);
        }

        public override void TriggerReset()
        {
            _controller.Clear();
        }

        private void AddDOT(WeaponPreHitEnemyContext context, float triggerAmt)
        {
            Dam_EnemyDamageLimb? limb = context.Damageable.TryCast<Dam_EnemyDamageLimb>();
            if (limb == null || limb.m_armorDamageMulti == 0 || limb.m_base.IsImortal == true) return;
            float falloff = IgnoreFalloff ? 1f : context.Falloff;
            float damage = TotalDamage;
            float backstabMulti = IgnoreBackstab ? 1f : context.Backstab;
            float precisionMulti = PrecisionDamageMulti;

            CustomWeaponComponent? cwc = Weapon?.GetComponent<CustomWeaponComponent>();
            if (cwc != null)
            {
                WeaponDamageContext damageContext = new(damage, precisionMulti, context.Damageable, cwc.Weapon);
                cwc.Invoke(context);
                if (!IgnoreDamageMods)
                    damage = damageContext.Damage.Value;
                precisionMulti = damageContext.Precision.Value;
            }

            EXPAPIWrapper.ApplyMod(ref damage);

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
                    lastDot = _controller.AddDOT(damage, falloff, precisionMulti, backstabMulti, context.Damageable, this);
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
                    DOTInstance? newDOT = _controller.AddDOT(damage, falloff, precisionMulti, backstabMulti, context.Damageable, this);
                    if (newDOT != null)
                        _lastDOTs[new AgentWrapper(limb.GetBaseAgent())] = newDOT;
                }
            }
            else
                _controller.AddDOT(damage, falloff, precisionMulti, backstabMulti, context.Damageable, this);
        }

        public override IWeaponProperty Clone()
        {
            DamageOverTime copy = new()
            {
                TotalDamage = TotalDamage,
                PrecisionDamageMulti = PrecisionDamageMulti,
                StaggerDamageMulti = StaggerDamageMulti,
                Duration = Duration,
                Stacks = Stacks,
                IgnoreFalloff = IgnoreFalloff,
                DamageLimb = DamageLimb,
                IgnoreArmor = IgnoreArmor,
                IgnoreBackstab = IgnoreBackstab,
                IgnoreDamageMods = IgnoreDamageMods,
                TickRate = TickRate,
                Trigger = Trigger?.Clone()
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TotalDamage), TotalDamage);
            writer.WriteNumber(nameof(PrecisionDamageMulti), PrecisionDamageMulti);
            writer.WriteNumber(nameof(StaggerDamageMulti), StaggerDamageMulti);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteNumber(nameof(TickRate), TickRate);
            writer.WriteBoolean(nameof(Stacks), Stacks);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreDamageMods), IgnoreDamageMods);
            SerializeTrigger(writer, options);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            base.DeserializeProperty(property, ref reader, options);
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
                default:
                    break;
            }
        }
    }
}
