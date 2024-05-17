using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Player;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DamageOverTime : IWeaponProperty<WeaponPreHitEnemyContext>
    {
        public readonly static string Name = typeof(DamageOverTime).Name;
        public bool AllowStack { get; } = true;
        public PlayerAgent? Owner { get; set; }

        public float TotalDamage { get; set; } = 0f;
        public float PrecisionMult { get; set; } = 0f;
        public float StaggerMult { get; set; } = 0f;
        public float Duration { get; set; } = 0f;
        public bool Stacks { get; set; } = true;
        public bool IgnoreFalloff { get; set; } = false;
        public bool DamageLimb { get; set; } = true;
        public bool IgnoreArmor { get; set; } = false;
        private float _tickRate = 2f;
        public float TickRate
        {
            get { return _tickRate; }
            set { _tickRate = MathF.Max(0.01f, value); }
        }
        private readonly DOTController _controller = new();
        private DOTInstance? _lastDOT = null;

        public void Invoke(WeaponPreHitEnemyContext context)
        {
            if (Owner == null)
                Owner = context.Weapon.Owner;

            Dam_EnemyDamageLimb? limb = context.Damageable.TryCast<Dam_EnemyDamageLimb>();
            if (limb == null || limb.m_armorDamageMulti == 0 || limb.m_base.IsImortal == true) return;
            float damage = TotalDamage * (IgnoreFalloff ? 1f : context.Falloff);

            // If it doesn't stack and we already added one, set the new one with the existing next tick time
            if (!Stacks && _lastDOT != null && !_lastDOT.Expired)
            {
                float nextTickTime = _lastDOT.NextTickTime;
                _lastDOT.Destroy();
                _lastDOT = _controller.AddDOT(damage, context.Damageable, this);
                _lastDOT?.StartWithTargetTime(nextTickTime);
            }
            else
                _lastDOT = _controller.AddDOT(damage, context.Damageable, this);
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(TotalDamage), TotalDamage);
            writer.WriteNumber(nameof(PrecisionMult), PrecisionMult);
            writer.WriteNumber(nameof(StaggerMult), StaggerMult);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteNumber(nameof(TickRate), TickRate);
            writer.WriteBoolean(nameof(Stacks), Stacks);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
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
                default:
                    break;
            }
        }
    }
}
