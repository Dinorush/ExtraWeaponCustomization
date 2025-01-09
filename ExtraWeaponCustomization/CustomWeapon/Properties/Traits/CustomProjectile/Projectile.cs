using EWC.CustomWeapon.Properties.Traits.CustomProjectile;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Log;
using FX_EffectSystem;
using Gear;
using SNetwork;
using System;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class Projectile :
        Trait,
        IGunProperty,
        ISyncProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponPostRayContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
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
        public bool RunHitTriggers { get; private set; } = true;
        public float VisualLerpDist { get; private set; } = 5f;
        public float Lifetime { get; private set; } = 20f;

        public ProjectileHomingSettings HomingSettings { get; private set; } = new();

        private float _cachedRayDist;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;

        public void Invoke(WeaponSetupContext context)
        {
            _cachedRayDist = CWC.Gun!.MaxRayDist;
            CWC.Gun!.MaxRayDist = 1f; // Non-zero so piercing weapons don't break
        }

        public void Invoke(WeaponClearContext context)
        {
            CWC.Gun!.MaxRayDist = _cachedRayDist;
        }

        public void Invoke(WeaponPostRayContext context)
        {
            if (!CWC.Gun!.Owner.IsLocallyOwned && (!SNet.IsMaster || CWC.Gun!.Owner.Owner.IsBot)) return;

            context.Result = false;
            context.Data.maxRayDist = 0f;

            s_ray.origin = context.Position;
            s_ray.direction = context.Data.fireDir;
            float visualDist = VisualLerpDist > 0.1f ? VisualLerpDist : 0.1f;
            if (Physics.Raycast(s_ray, out s_rayHit, visualDist, LayerUtil.MaskEntityAndWorld3P))
                visualDist = s_rayHit.distance;

            Vector3 position = context.Position + context.Data.fireDir * Math.Min(visualDist, 0.1f);

            // Deprecated run triggers - remove later
            if (!RunHitTriggers)
                CWC.RunHitTriggers = false;
            var comp = EWCProjectileManager.Shooter.CreateAndSendProjectile(this, position, context.Data.fireDir);
            if (!RunHitTriggers)
                CWC.RunHitTriggers = true;
            if (comp == null)
            {
                EWCLogger.Error("Unable to create shooter projectile!");
                return;
            }

            if (VisualLerpDist > 0)
                comp.SetVisualPosition(CWC.Gun!.MuzzleAlign.position, visualDist);
            comp.Hitbox.HitEnts.Add(context.IgnoreEnt);
        }

        // Cancel tracer FX
        public void Invoke(WeaponPostFireContext context)
        {
            if (!CWC.Gun!.Owner.IsLocallyOwned) return;

            CancelTracerFX(CWC.Gun!, CWC.Gun!.TryCast<Shotgun>() != null);
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            CancelTracerFX(CWC.Gun!, CWC.Gun!.TryCast<ShotgunSynced>() != null);
        }

        public static void CancelTracerFX(BulletWeapon weapon, bool isShotgun)
        {
            int shots = 1;
            if (isShotgun)
                shots = weapon.ArchetypeData.ShotgunBulletCount;

            for (int i = 0; i < shots; i++)
            {
                var effect = BulletWeapon.s_tracerPool.m_inUse[^1].TryCast<FX_Effect>();
                if (effect == null) return; // JFS - Shouldn't happen

                foreach (var link in effect.m_links)
                    link.TryCast<FX_EffectLink>()!.m_playingEffect?.ReturnToPool();

                effect.ReturnToPool();
            }
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
            writer.WriteNumber(nameof(HitSizeWorld), HitSizeWorld);
            writer.WriteNumber(nameof(ModelScale), ModelScale);
            writer.WriteBoolean(nameof(EnableTrail), EnableTrail);
            writer.WritePropertyName(nameof(TrailColor));
            EWCJson.Serialize(writer, TrailColor);
            writer.WriteNumber(nameof(TrailWidth), TrailWidth);
            writer.WriteNumber(nameof(TrailTime), TrailTime);
            writer.WriteBoolean(nameof(TrailCullOnDie), TrailCullOnDie);
            writer.WritePropertyName(nameof(GlowColor));
            EWCJson.Serialize(writer, GlowColor);
            writer.WriteNumber(nameof(GlowIntensity), GlowIntensity);
            writer.WriteNumber(nameof(GlowRange), GlowRange);
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
            writer.WriteBoolean(nameof(HitFromOwnerPos), HitFromOwnerPos);
            writer.WriteNumber(nameof(HitCooldown), HitCooldown);
            writer.WriteNumber(nameof(HitIgnoreWallsDuration), HitIgnoreWallsDuration);
            writer.WriteBoolean(nameof(RunHitTriggers), RunHitTriggers);
            writer.WriteNumber(nameof(VisualLerpDist), VisualLerpDist);
            writer.WriteNumber(nameof(Lifetime), Lifetime);
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
                case "runhittriggers":
                case "hittriggers":
                    if (!reader.GetBoolean())
                    {
                        EWCLogger.Warning("Projectile field \"RunHitTriggers\" is deprecated and will be removed in the future. Set the field on MultiShot or FireShot instead.");
                        RunHitTriggers = false;
                    }
                    break;
                case "visuallerpdist":
                case "lerpdist":
                    VisualLerpDist = reader.GetSingle();
                    break;
                case "maxlifetime":
                case "lifetime":
                    Lifetime = reader.GetSingle();
                    break;

                case "homingsettings":
                case "homing":
                    HomingSettings.Deserialize(ref reader);
                    break;
                default:
                    break;
            }
        }
    }
}
