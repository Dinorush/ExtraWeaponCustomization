using AK;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
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
        public bool DamageFriendly { get; set; } = true;
        public bool DamageOwner { get; set; } = true;
        public uint SoundID { get; set; } = EVENTS.STICKYMINEEXPLODE;
        public Color GlowColor { get; set; } = ExplosionManager.FlashColor;
        public float GlowDuration { get; set; } = 0.05f;

        public float CacheBackstab { get; private set; } = 0f;
        public CustomWeaponComponent CWC { get; private set; }
        public BulletWeapon Weapon { get; private set; }

#pragma warning disable CS8618
        public Explosive()
        {
            Trigger ??= new(ITrigger.GetTrigger(ITrigger.BulletLanded)!);
            SetValidTriggers(DamageType.Explosive, ITrigger.Hit, ITrigger.BulletLanded, ITrigger.Kill);
        }
#pragma warning restore CS8618

        public override void TriggerReset() {}
        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            if (Weapon == null)
            {
                Weapon = triggerList[0].context.Weapon;
                CWC = Weapon.GetComponent<CustomWeaponComponent>();
            }

            foreach (TriggerContext tContext in triggerList)
            {
                // Fix bug where explosion and gun can have same search ID, causing gun to deal no damage
                if (Weapon.m_damageSearchID > 0 && Weapon.m_damageSearchID - 1 == DamageUtil.SearchID)
                    DamageUtil.IncrementSearchID();

                if (tContext.context is WeaponPostKillContext killContext)
                {
                    CacheBackstab = killContext.Backstab;
                    ExplosionManager.DoExplosion(killContext.Position, killContext.Direction, Weapon.Owner, IgnoreFalloff ? 1f : killContext.Falloff, this, null);
                }
                else
                {
                    CacheBackstab = 0f;
                    WeaponPreHitContext context = (WeaponPreHitContext)tContext.context;
                    Vector3 position = context.Position;
                    if (context.Damageable != null)
                        position = context.LocalPosition + context.Damageable.GetBaseAgent().Position;

                    if (context is WeaponPreHitEnemyContext enemyContext)
                        CacheBackstab = enemyContext.Backstab;

                    ExplosionManager.DoExplosion(position, context.Direction, Weapon.Owner, IgnoreFalloff ? 1f : context.Falloff, this, context.Damageable);
                }
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
                DamageFriendly = DamageFriendly,
                DamageOwner = DamageOwner,
                SoundID = SoundID,
                GlowColor = GlowColor,
                GlowDuration = GlowDuration,
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
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
            writer.WriteNumber(nameof(SoundID), SoundID);
            writer.WritePropertyName(nameof(GlowColor));
            JsonSerializer.Serialize(writer, GlowColor, options);
            writer.WriteNumber(nameof(GlowDuration), GlowDuration);
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
                case "damagefriendly":
                case "friendlyfire":
                    DamageFriendly = reader.GetBoolean();
                    break;
                case "damageowner":
                case "damageuser":
                    DamageOwner = reader.GetBoolean();
                    break;
                case "soundid":
                case "sound":
                    SoundID = reader.GetUInt32();
                    break;
                case "glowcolor":
                case "color":
                    GlowColor = JsonSerializer.Deserialize<Color>(ref reader, options);
                    break;
                case "glowduration":
                case "duration":
                    GlowDuration = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
