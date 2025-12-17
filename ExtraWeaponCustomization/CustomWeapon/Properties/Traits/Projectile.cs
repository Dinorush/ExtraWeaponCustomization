using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class Projectile :
        Trait,
        ITriggerEvent,
        ISyncProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        public ushort SyncPropertyID { get; set; }

        public ProjectileType ProjectileType { get; private set; } = ProjectileType.NotTargetingSmallFast;
        public float MinSpeed { get; private set; } = 0f;
        public float MaxSpeed { get; private set; } = 0f;
        public float AccelScale { get; private set; } = 1f;
        public float AccelExponent { get; private set; } = 1f;
        private float _accelTime = 0.001f;
        public float AccelTime
        {
            get { return _accelTime; }
            private set { _accelTime = Math.Max(0.001f, value); }
        }
        public float Gravity { get; private set; } = 0f;
        public float HitSize { get; private set; } = 0f;
        public float HitSizeFriendly { get; private set; } = 0f;
        public float HitSizeWorld { get; private set; } = 0f;
        public float ModelScale { get; private set; } = 1f;
        public bool EnableTrail { get; private set; } = true;
        public Color TrailColor { get; private set; } = Color.black;
        public float TrailWidth { get; private set; } = -1f;
        public float TrailTime { get; private set; } = -1f;
        public bool TrailCullOnDie { get; private set; } = true;
        public Color GlowColor { get; private set; } = Color.black;
        public float GlowIntensity { get; private set; } = 1f;
        public float GlowRange { get; private set; } = -1f;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = false;
        public bool HitFromOwnerPos { get; private set; } = false;
        public float HitCooldown { get; private set; } = -1;
        public float HitIgnoreWallsDuration { get; private set; } = 0f;
        public int RicochetCount { get; private set; } = 0;
        public float RicochetSpeedMod { get; private set; } = 1f;
        public float RicochetSpeedAngleFactor { get; private set; } = 0f;
        public bool RicochetOnHit { get; private set; } = false;
        public bool EnableTerrainHitFX { get; private set; } = true;
        public float HitCheckCooldown { get; private set; } = 0.05f;
        public float VisualLerpDist { get; private set; } = 5f;
        public float Lifetime { get; private set; } = 20f;
        public uint FlyingSoundID { get; private set; } = 0u;
        public uint DestroyedSoundID { get; private set; } = 0u;
        public bool StopFlyingSoundOnDestroy { get; private set; } = false;
        public float AliveTriggerDelay { get; private set; } = 0f;
        public float AliveTriggerInterval { get; private set; } = 1f;

        public List<ProjectileDirChange> DirChanges { get; private set; } = new();
        public List<ProjectileStatChange> StatChanges { get; private set; } = new();
        public ProjectileHomingSettings HomingSettings { get; private set; } = new();

        // Used by Shrapnel to override properties that the hitbox uses

        private Func<HitData, ContextController, bool>? _hitFuncOverride = null;
        public Func<HitData, ContextController, bool>? HitFuncOverride => _hitFuncOverride;

        private WallPierce? _wallPierceOverride = null;
        public WallPierce? WallPierce => _useOverrides ? _wallPierceOverride : CWC?.GetTrait<WallPierce>();

        private bool _useOverrides = false;
        private float _cachedRayDist;
        private ushort _shotIndex = 0;

        private static RaycastHit s_rayHit;
        public readonly TriggerEventHelper EventHelper = new(CallbackMap);

        public int GetCallbackID(string callbackName) => EventHelper.GetCallbackID(callbackName);

        public void Invoke(WeaponSetupContext context)
        {
            _cachedRayDist = CWC.Weapon.MaxRayDist;
            CWC.Weapon.MaxRayDist = 1f; // Non-zero so piercing weapons don't break
            CWC.ShotComponent.Projectile = this;
        }

        public void Invoke(WeaponClearContext context)
        {
            CWC.Weapon.MaxRayDist = _cachedRayDist;
            CWC.ShotComponent.Projectile = null;
        }

        public void Fire(Ray ray, Vector3 fxPos, HitData hitData, IntPtr ignoreEnt)
        {
            float visualDist = VisualLerpDist > 0.1f ? VisualLerpDist : 0.1f;
            if (Physics.Raycast(ray, out s_rayHit, visualDist, LayerUtil.MaskEntityAndWorld3P))
                visualDist = s_rayHit.distance;

            Vector3 position = ray.origin + ray.direction * Math.Min(visualDist, 0.1f);
            var comp = EWCProjectileManager.Shooter.CreateAndSendProjectile(this, _shotIndex++, position, fxPos, hitData, ignoreEnt);
            if (comp == null)
            {
                EWCLogger.Error("Unable to create shooter projectile!");
                return;
            }

            if (VisualLerpDist > 0 && fxPos != Vector3.zero)
                comp.SetVisualPosition(fxPos, visualDist);
        }

        internal void SetOverrides(Func<HitData, ContextController, bool>? hitFunc = null, WallPierce? wallPierce = null)
        {
            _useOverrides = true;
            _hitFuncOverride = hitFunc;
            _wallPierceOverride = wallPierce;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteString(nameof(ProjectileType), ProjectileType.ToString());
            writer.WriteNumber(nameof(MinSpeed), MinSpeed);
            writer.WriteNumber(nameof(MaxSpeed), MaxSpeed);
            writer.WriteNumber(nameof(AccelScale), AccelScale);
            writer.WriteNumber(nameof(AccelExponent), AccelExponent);
            writer.WriteNumber(nameof(AccelTime), AccelTime);
            writer.WriteNumber(nameof(Gravity), Gravity);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteNumber(nameof(HitSizeFriendly), HitSizeFriendly);
            writer.WriteNumber(nameof(HitSizeWorld), HitSizeWorld);
            writer.WriteNumber(nameof(ModelScale), ModelScale);
            writer.WriteBoolean(nameof(EnableTrail), EnableTrail);
            EWCJson.Serialize(writer, nameof(TrailColor), TrailColor);
            writer.WriteNumber(nameof(TrailWidth), TrailWidth);
            writer.WriteNumber(nameof(TrailTime), TrailTime);
            writer.WriteBoolean(nameof(TrailCullOnDie), TrailCullOnDie);
            EWCJson.Serialize(writer, nameof(GlowColor), GlowColor);
            writer.WriteNumber(nameof(GlowIntensity), GlowIntensity);
            writer.WriteNumber(nameof(GlowRange), GlowRange);
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
            writer.WriteBoolean(nameof(HitFromOwnerPos), HitFromOwnerPos);
            writer.WriteNumber(nameof(HitCooldown), HitCooldown);
            writer.WriteNumber(nameof(HitIgnoreWallsDuration), HitIgnoreWallsDuration);
            writer.WriteNumber(nameof(RicochetCount), RicochetCount);
            writer.WriteNumber(nameof(RicochetSpeedMod), RicochetSpeedMod);
            writer.WriteNumber(nameof(RicochetSpeedAngleFactor), RicochetSpeedAngleFactor);
            writer.WriteBoolean(nameof(RicochetOnHit), RicochetOnHit);
            writer.WriteBoolean(nameof(EnableTerrainHitFX), EnableTerrainHitFX);
            writer.WriteNumber(nameof(HitCheckCooldown), HitCheckCooldown);
            writer.WriteNumber(nameof(VisualLerpDist), VisualLerpDist);
            writer.WriteNumber(nameof(Lifetime), Lifetime);
            writer.WriteNumber(nameof(FlyingSoundID), FlyingSoundID);
            writer.WriteNumber(nameof(DestroyedSoundID), DestroyedSoundID);
            writer.WriteBoolean(nameof(StopFlyingSoundOnDestroy), StopFlyingSoundOnDestroy);
            writer.WriteNumber(nameof(AliveTriggerDelay), AliveTriggerDelay);
            writer.WriteNumber(nameof(AliveTriggerInterval), AliveTriggerInterval);
            writer.WritePropertyName(nameof(DirChanges));
            ProjectileDirChange.SerializeList(DirChanges, writer);
            writer.WritePropertyName(nameof(StatChanges));
            ProjectileStatChange.SerializeList(StatChanges, writer);
            writer.WritePropertyName(nameof(HomingSettings));
            HomingSettings.Serialize(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property.ToLowerInvariant())
            {
                case "projectiletype":
                case "type":
                    ProjectileType = reader.GetString().ToEnum(ProjectileType.NotTargetingSmallFast);
                    if (ProjectileType == ProjectileType.GlueFlying || ProjectileType == ProjectileType.GlueLanded)
                        ProjectileType = ProjectileType.NotTargetingSmallFast;
                    break;
                case "minspeed":
                case "speed":
                    MinSpeed = reader.GetSingle();
                    break;
                case "maxspeed":
                    MaxSpeed = reader.GetSingle();
                    break;
                case "accelscale":
                case "accel":
                    AccelScale = reader.GetSingle();
                    break;
                case "accelexponent":
                case "accelexpo":
                    AccelExponent = reader.GetSingle();
                    break;
                case "acceltime":
                    AccelTime = reader.GetSingle();
                    break;
                case "gravity":
                    Gravity = reader.GetSingle();
                    break;
                case "hitsize":
                case "size":
                    HitSize = Math.Max(0, reader.GetSingle());
                    break;
                case "hitsizefriendly":
                case "sizefriendly":
                    HitSizeFriendly = Math.Max(0, reader.GetSingle());
                    break;
                case "hitsizeworld":
                case "sizeworld":
                    HitSizeWorld = Math.Max(0, reader.GetSingle());
                    break;
                case "modelscale":
                case "scale":
                    ModelScale = Math.Max(0, reader.GetSingle());
                    break;
                case "enabletrail":
                case "trail":
                    EnableTrail = reader.GetBoolean();
                    break;
                case "trailcolor":
                    TrailColor = EWCJson.Deserialize<Color>(ref reader);
                    break;
                case "trailwidth":
                    TrailWidth = reader.GetSingle();
                    break;
                case "trailtime":
                    TrailTime = reader.GetSingle();
                    break;
                case "trailcullondie":
                case "trailcull":
                    TrailCullOnDie = reader.GetBoolean();
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
                case "damagefriendly":
                case "friendlyfire":
                    DamageFriendly = reader.GetBoolean();
                    break;
                case "damageowner":
                case "damageuser":
                    DamageOwner = reader.GetBoolean();
                    break;
                case "hitfromownerposition":
                case "hitfromownerpos":
                    HitFromOwnerPos = reader.GetBoolean();
                    break;
                case "hitcooldown":
                    HitCooldown = reader.GetSingle();
                    break;
                case "hitignorewallsduration":
                    HitIgnoreWallsDuration = reader.GetSingle();
                    break;
                case "ricochetcount":
                case "ricochet":
                    RicochetCount = reader.GetInt32();
                    break;
                case "ricochetspeedmod":
                    RicochetSpeedMod = reader.GetSingle();
                    break;
                case "ricochetspeedanglefactor":
                    RicochetSpeedAngleFactor = reader.GetSingle();
                    break;
                case "ricochetonhit":
                    RicochetOnHit = reader.GetBoolean();
                    break;
                case "enableterrainhitfx":
                case "enableterrainfx":
                    EnableTerrainHitFX = reader.GetBoolean();
                    break;
                case "hitcheckcooldown":
                    HitCheckCooldown = reader.GetSingle();
                    break;
                case "visuallerpdist":
                case "lerpdist":
                    VisualLerpDist = reader.GetSingle();
                    break;
                case "maxlifetime":
                case "lifetime":
                    Lifetime = reader.GetSingle();
                    break;
                case "flyingsoundid":
                case "flyingsound":
                    if (reader.TokenType == JsonTokenType.String)
                        FlyingSoundID = AkSoundEngine.GetIDFromString(reader.GetString()!);
                    else
                        FlyingSoundID = reader.GetUInt32();
                    break;
                case "destroyedsoundid":
                case "destroyedsound":
                    if (reader.TokenType == JsonTokenType.String)
                        DestroyedSoundID = AkSoundEngine.GetIDFromString(reader.GetString()!);
                    else
                        DestroyedSoundID = reader.GetUInt32();
                    break;
                case "stopflyingsoundondestroy":
                    StopFlyingSoundOnDestroy = reader.GetBoolean();
                    break;
                case "alivetriggerdelay":
                    AliveTriggerDelay = reader.GetSingle();
                    break;
                case "alivetriggerinterval":
                    AliveTriggerInterval = reader.GetSingle();
                    break;
                case "dirchanges":
                    DirChanges = ProjectileDirChange.DeserializeList(ref reader);
                    break;
                case "statchanges":
                    StatChanges = ProjectileStatChange.DeserializeList(ref reader);
                    break;
                case "homingsettings":
                case "homing":
                    HomingSettings.Deserialize(ref reader);
                    break;
                default:
                    break;
            }
        }

        private static int CallbackMap(string callback) => callback switch
        {
            "alive" => (int)Callback.Alive,
            "destroyed" => (int)Callback.Destroyed,
            _ => 0
        };

        public enum Callback
        {
            Alive,
            Destroyed
        }
    }
}
