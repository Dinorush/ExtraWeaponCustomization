using AK;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Explosive : 
        Effect,
        IGunProperty,
        IMeleeProperty
    {
        public float MaxDamage { get; private set; } = 0f;
        public float MinDamage { get; private set; } = 0f;
        public float InnerRadius { get; private set; } = 0f;
        public float Radius { get; private set; } = 0f;
        public float PrecisionDamageMulti { get; private set; } = 0f;
        public float StaggerDamageMulti { get; private set; } = 1f;
        public bool IgnoreFalloff { get; private set; } = false;
        public bool DamageLimb { get; private set; } = true;
        public bool IgnoreArmor { get; private set; } = false;
        public bool IgnoreBackstab { get; private set; } = false;
        public bool IgnoreDamageMods { get; private set; } = false;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = true;
        public uint SoundID { get; private set; } = EVENTS.STICKYMINEEXPLODE;
        public Color GlowColor { get; private set; } = new(1, 0.2f, 0, 1);
        public float GlowDuration { get; private set; } = 0.1f;
        public float GlowFadeDuration { get; private set; } = 0.1f;

        public float CacheBackstab { get; private set; } = 0f;

        public Explosive()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.BulletLanded));
            SetValidTriggers(DamageType.Explosive, TriggerName.BulletLanded, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge, TriggerName.Kill);
        }

        public override void TriggerReset() {}
        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            foreach (TriggerContext tContext in triggerList)
            {
                // Fix bug where explosion and gun can have same search ID, causing gun to deal no damage
                if (CWC.IsGun && CWC.Gun!.m_damageSearchID > 0 && CWC.Gun.m_damageSearchID - 1 == DamageUtil.SearchID)
                    DamageUtil.IncrementSearchID();

                if (tContext.context is WeaponPostKillContext killContext)
                {
                    CacheBackstab = killContext.Backstab;
                    ExplosionManager.DoExplosion(killContext.Position, killContext.Direction, CWC.Weapon.Owner, IgnoreFalloff ? 1f : killContext.Falloff, this, tContext.triggerAmt, null);
                }
                else
                {
                    CacheBackstab = 0f;
                    WeaponPreHitContext context = (WeaponPreHitContext)tContext.context;
                    Vector3 position = context.Position;
                    if (context.Damageable != null && context.Damageable.GetBaseAgent() != null)
                        position = context.LocalPosition + context.Damageable.GetBaseAgent().Position;

                    if (context is WeaponPreHitEnemyContext enemyContext)
                        CacheBackstab = enemyContext.Backstab;

                    ExplosionManager.DoExplosion(position, context.Direction, CWC.Weapon.Owner, IgnoreFalloff ? 1f : context.Falloff, this, tContext.triggerAmt, context.Damageable);
                }
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
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
            SerializeTrigger(writer);
            writer.WriteNumber(nameof(SoundID), SoundID);
            writer.WritePropertyName(nameof(GlowColor));
            EWCJson.Serialize(writer, GlowColor);
            writer.WriteNumber(nameof(GlowDuration), GlowDuration);
            writer.WriteNumber(nameof(GlowFadeDuration), GlowFadeDuration);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
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
                    if (reader.TokenType == JsonTokenType.String)
                        SoundID = AkSoundEngine.GetIDFromString(reader.GetString()!);
                    else
                        SoundID = reader.GetUInt32();
                    break;
                case "glowcolor":
                case "color":
                    GlowColor = EWCJson.Deserialize<Color>(ref reader);
                    break;
                case "glowduration":
                case "duration":
                    GlowDuration = reader.GetSingle();
                    break;
                case "glowfadeduration":
                case "fadeduration":
                    GlowFadeDuration = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
