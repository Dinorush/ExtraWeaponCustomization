using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class Explosive : 
        Effect
    {
        public float MaxDamage { get; set; } = 0f;
        public float MinDamage { get; set; } = 0f;
        public float InnerRadius { get; set; } = 0f;
        public float Radius { get; set; } = 0f;
        public float PrecisionDamageMulti { get; set; } = 0f;
        public float StaggerDamageMulti { get; set; } = 1f;
        public bool IgnoreFalloff { get; set; } = false;
        public bool DamageLimb { get; set; } = true;
        public bool IgnoreArmor { get; set; } = false;
        public bool IgnoreBackstab { get; set; } = false;
        public bool IgnoreDamageMods { get; set; } = false;

        public float CacheBackstab { get; private set; } = 0f;

        public Explosive()
        {
            Trigger ??= new(ITrigger.GetTrigger(ITrigger.BulletLanded)!);
            SetValidTriggers(DamageType.Explosive, ITrigger.Hit, ITrigger.Damage, ITrigger.BulletLanded);
        }

        public override void TriggerReset() {}
        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            foreach (TriggerContext tContext in triggerList)
            {
                CacheBackstab = 1f;
                WeaponPreHitContext context = (WeaponPreHitContext) tContext.context;
                Vector3 position = context.Position;
                if (context.Damageable != null)
                    position = context.LocalPosition + context.Damageable.GetBaseAgent().Position;
                if (context is WeaponPreHitEnemyContext enemyContext)
                {
                    CacheBackstab = enemyContext.Backstab;
                    // Fix bug where explosion and gun can have same search ID, causing gun to deal no damage
                    if (context.Weapon.m_damageSearchID - 1 == DamageUtil.SearchID)
                        DamageUtil.IncrementSearchID();
                }
                ExplosionManager.DoExplosion(position, context.Direction, context.Weapon.Owner, IgnoreFalloff ? 1f : context.Falloff, this, context.Weapon, context.Damageable);
            }
        }

        public override IWeaponProperty Clone()
        {
            Explosive copy = new()
            {
                MaxDamage = MaxDamage,
                MinDamage = MinDamage,
                InnerRadius = InnerRadius,
                Radius = Radius,
                PrecisionDamageMulti = PrecisionDamageMulti,
                StaggerDamageMulti = StaggerDamageMulti,
                DamageLimb = DamageLimb,
                IgnoreArmor = IgnoreArmor,
                IgnoreFalloff = IgnoreFalloff,
                IgnoreBackstab = IgnoreBackstab,
                IgnoreDamageMods = IgnoreDamageMods,
                Trigger = Trigger?.Clone()
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(MaxDamage), MaxDamage);
            writer.WriteNumber(nameof(MinDamage), MinDamage);
            writer.WriteNumber(nameof(InnerRadius), InnerRadius);
            writer.WriteNumber(nameof(Radius), Radius);
            writer.WriteNumber(nameof(PrecisionDamageMulti), PrecisionDamageMulti);
            writer.WriteNumber(nameof(StaggerDamageMulti), StaggerDamageMulti);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreDamageMods), IgnoreDamageMods);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            base.DeserializeProperty(property, ref reader, options);
            switch (property)
            {
                case "maxdamage":
                    MaxDamage = reader.GetSingle();
                    break;
                case "mindamage":
                    MinDamage = reader.GetSingle();
                    break;
                case "innerradius":
                case "minradius":
                    InnerRadius = reader.GetSingle();
                    break;
                case "radius":
                case "maxradius":
                    Radius = reader.GetSingle();
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
