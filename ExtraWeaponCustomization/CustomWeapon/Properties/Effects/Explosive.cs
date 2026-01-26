using AK;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Hit.Explosion;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils.Extensions;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Explosive : 
        Effect,
        ISyncProperty
    {
        public ushort SyncPropertyID { get; set; }

        public float MaxDamage { get; private set; } = 0f;
        public float MinDamage { get; private set; } = 0f;
        public float InnerRadius { get; private set; } = 0f;
        public float Radius { get; private set; } = 0f;
        public float Exponent { get; private set; } = 1f;
        public float PrecisionDamageMulti { get; private set; } = 0f;
        public float StaggerDamageMulti { get; private set; } = 1f;
        public float FriendlyDamageMulti { get; private set; } = 1f;
        public bool IgnoreFalloff { get; private set; } = false;
        public bool DamageLimb { get; private set; } = true;
        public bool IgnoreArmor { get; private set; } = false;
        public bool IgnoreBackstab { get; private set; } = false;
        public bool IgnoreShotMods { get; private set; } = false;
        public bool UseParentShotMod { get; private set; } = true;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = true;
        public bool DamageLocks { get; private set; } = true;
        public bool HitFromExplosionPos { get; private set; } = false;
        public bool HitClosestFirst { get; private set; } = false;
        public bool ApplyAttackCooldown { get; private set; } = true;
        public TriggerPosMode ApplyPositionMode { get; private set; } = TriggerPosMode.Relative;
        public uint SoundID { get; private set; } = EVENTS.STICKYMINEEXPLODE;
        public bool EnableMineFX { get; private set; } = false;
        public Color GlowColor { get; private set; } = new(1, 0.2f, 0, 1);
        public float GlowIntensity { get; private set; } = 5f;
        public float GlowDuration { get; private set; } = 0.1f;
        public float GlowFadeDuration { get; private set; } = 0.1f;
        public float ScreenShakeIntensity { get; private set; } = 0f;
        public float ScreenShakeFrequency { get; private set; } = 30f;
        public float ScreenShakeDuration { get; private set; } = 0f;
        public float ScreenShakeInnerRadius { get; private set; } = 0f;
        public float ScreenShakeRadius { get; private set; } = 0f;

        public float CacheBackstab { get; private set; } = 0f;

        private const float WallHitBuffer = 0.03f;

        public Explosive()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.BulletLanded));
            SetValidTriggers(DamageType.Explosive);
        }

        public override void TriggerReset() {}
        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            foreach (TriggerContext tContext in triggerList)
            {
                CacheBackstab = 0f;
                if(ApplyPositionMode != TriggerPosMode.User && tContext.context is IPositionContext hitContext)
                {
                    Vector3 position = hitContext.Position;
                    if (hitContext is WeaponHitDamageableContextBase damContext)
                    {
                        CacheBackstab = damContext.Backstab;
                        Agents.Agent? agent = damContext.Damageable.GetBaseAgent();
                        if (ApplyPositionMode == TriggerPosMode.Relative && agent != null)
                            position = damContext.LocalPosition + agent.Position;
                    }
                    else
                        position += hitContext.Normal * WallHitBuffer;

                    ExplosionManager.DoExplosion(position, hitContext.Direction, hitContext.Normal, IgnoreFalloff ? 1f : hitContext.Falloff, this, tContext.triggerAmt, hitContext.ShotInfo.Orig);
                }
                else
                {
                    var owner = CWC.Owner;
                    ExplosionManager.DoExplosion(owner.FirePos, owner.FireDir, owner.FireDir, 1f, this, tContext.triggerAmt);
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
            writer.WriteNumber(nameof(Exponent), Exponent);
            writer.WriteNumber(nameof(PrecisionDamageMulti), PrecisionDamageMulti);
            writer.WriteNumber(nameof(StaggerDamageMulti), StaggerDamageMulti);
            writer.WriteNumber(nameof(FriendlyDamageMulti), FriendlyDamageMulti);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreShotMods), IgnoreShotMods);
            writer.WriteBoolean(nameof(UseParentShotMod), UseParentShotMod);
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
            writer.WriteBoolean(nameof(DamageLocks), DamageLocks);
            writer.WriteBoolean(nameof(HitFromExplosionPos), HitFromExplosionPos);
            writer.WriteBoolean(nameof(HitClosestFirst), HitClosestFirst);
            writer.WriteBoolean(nameof(ApplyAttackCooldown), ApplyAttackCooldown);
            writer.WriteString(nameof(ApplyPositionMode), ApplyPositionMode.ToString());
            SerializeTrigger(writer);
            writer.WriteNumber(nameof(SoundID), SoundID);
            writer.WriteBoolean(nameof(EnableMineFX), EnableMineFX);
            EWCJson.Serialize(writer, nameof(GlowColor), GlowColor);
            writer.WriteNumber(nameof(GlowIntensity), GlowIntensity);
            writer.WriteNumber(nameof(GlowDuration), GlowDuration);
            writer.WriteNumber(nameof(GlowFadeDuration), GlowFadeDuration);
            writer.WriteNumber(nameof(ScreenShakeIntensity), ScreenShakeIntensity);
            writer.WriteNumber(nameof(ScreenShakeFrequency), ScreenShakeFrequency);
            writer.WriteNumber(nameof(ScreenShakeDuration), ScreenShakeDuration);
            writer.WriteNumber(nameof(ScreenShakeInnerRadius), ScreenShakeInnerRadius);
            writer.WriteNumber(nameof(ScreenShakeRadius), ScreenShakeRadius);
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
                case "exponent":
                    Exponent = reader.GetSingle();
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
                case "ignoreshotmods":
                case "ignoreshotmod":
                    IgnoreShotMods = reader.GetBoolean();
                    break;
                case "useparentshotmod":
                case "parentshotmod":
                    UseParentShotMod = reader.GetBoolean();
                    break;
                case "damagefriendly":
                case "friendlyfire":
                    DamageFriendly = reader.GetBoolean();
                    break;
                case "damageowner":
                case "damageuser":
                    DamageOwner = reader.GetBoolean();
                    break;
                case "damagelocks":
                    DamageLocks = reader.GetBoolean();
                    break;
                case "hitfromexplosionposition":
                case "hitfromexplosionpos":
                    HitFromExplosionPos = reader.GetBoolean();
                    break;
                case "hitclosestfirst":
                case "closestfirst":
                    HitClosestFirst = reader.GetBoolean();
                    break;
                case "applyattackcooldowns":
                case "applyattackcooldown":
                    ApplyAttackCooldown = reader.GetBoolean();
                    break;
                case "applypositionmode":
                    ApplyPositionMode = reader.GetString()!.ToEnum(TriggerPosMode.Relative);
                    break;
                case "applyonuser":
                    ApplyPositionMode = TriggerPosMode.User;
                    break;
                case "soundid":
                case "sound":
                    if (reader.TokenType == JsonTokenType.String)
                        SoundID = AkSoundEngine.GetIDFromString(reader.GetString()!);
                    else
                        SoundID = reader.GetUInt32();
                    break;
                case "enableminefx":
                case "minefx":
                    EnableMineFX = reader.GetBoolean();
                    break;
                case "glowcolor":
                case "color":
                    GlowColor = EWCJson.Deserialize<Color>(ref reader);
                    break;
                case "glowintensity":
                case "intensity":
                    GlowIntensity = reader.GetSingle();
                    break;
                case "glowduration":
                case "duration":
                    GlowDuration = reader.GetSingle();
                    break;
                case "glowfadeduration":
                case "fadeduration":
                    GlowFadeDuration = reader.GetSingle();
                    break;
                case "screenshakeintensity":
                case "shakeintensity":
                    ScreenShakeIntensity = reader.GetSingle();
                    break;
                case "screenshakefrequency":
                case "shakefrequency":
                    ScreenShakeFrequency = reader.GetSingle();
                    break;
                case "screenshakeduration":
                case "shakeduration":
                    ScreenShakeDuration = reader.GetSingle();
                    break;
                case "screenshakeinnerradius":
                case "shakeinnerradius":
                    ScreenShakeInnerRadius = reader.GetSingle();
                    break;
                case "screenshakeradius":
                case "shakeradius":
                    ScreenShakeRadius = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
