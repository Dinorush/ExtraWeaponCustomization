using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using FX_EffectSystem;
using Gear;
using SNetwork;
using System;
using System.Text.Json;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class Projectile :
        Trait,
        IWeaponProperty<WeaponPostSetupContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponPostRayContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        public ProjectileType ProjectileType { get; set; } = ProjectileType.NotTargetingSmallFast;
        public float Speed { get; set; } = 0f;
        public float AccelScale { get; set; } = 1f;
        public float AccelExponent { get; set; } = 1f;
        private float _accelTime = 0.001f;
        public float AccelTime
        {
            get { return _accelTime; }
            set { _accelTime = Math.Max(0.001f, value); }
        }
        public float Gravity { get; set; } = 0f;
        public float HitSize { get; set; } = 0f;
        public float HitSizeWorld { get; set; } = 0f;
        public float ModelScale { get; set; } = 1f;
        public bool EnableTrail { get; set; } = true;
        public Color GlowColor { get; set; } = Color.black;
        public float GlowRange { get; set; } = -1f;
        public bool DamageFriendly { get; set; } = true;
        public bool DamageOwner { get; set; } = false;
        public float VisualLerpDist { get; set; } = 5f;
        public float Lifetime { get; set; } = 20f;

        private CustomWeaponComponent? _cachedCWC;
        private float _cachedRayDist;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;

        public void Invoke(WeaponPostSetupContext context)
        {
            _cachedRayDist = context.Weapon.MaxRayDist;
            context.Weapon.MaxRayDist = 1f; // Non-zero so piercing weapons don't break
        }

        public void Invoke(WeaponClearContext context)
        {
            context.Weapon.MaxRayDist = _cachedRayDist;
        }

        public void Invoke(WeaponPostRayContext context)
        {
            if (!context.Weapon.Owner.IsLocallyOwned && (!SNet.IsMaster || context.Weapon.Owner.Owner.IsBot)) return;

            context.Result = false;
            context.Data.maxRayDist = 0f;

            s_ray.origin = context.Position;
            s_ray.direction = context.Data.fireDir;
            float visualDist = VisualLerpDist > 0.1f ? VisualLerpDist : 0.1f;
            if (Physics.Raycast(s_ray, out s_rayHit, visualDist, LayerManager.MASK_BULLETWEAPON_RAY))
                visualDist = s_rayHit.distance;

            Vector3 position = context.Position + context.Data.fireDir * Math.Min(visualDist, 0.1f);

            var comp = EWCProjectileManager.Shooter.CreateAndSendProjectile(this, position, context.Data.fireDir);
            if (comp == null)
            {
                EWCLogger.Error("Unable to create shooter projectile!");
                return;
            }

            _cachedCWC ??= context.Weapon.GetComponent<CustomWeaponComponent>();
            if (VisualLerpDist > 0)
                comp.SetVisualPosition(context.Weapon.MuzzleAlign.position, visualDist);
            comp.Hitbox.Init(_cachedCWC, this);
        }

        // Cancel tracer FX
        public void Invoke(WeaponPostFireContext context)
        {
            if (!context.Weapon.Owner.IsLocallyOwned && (!SNet.IsMaster || context.Weapon.Owner.Owner.IsBot)) return;

            CancelTracerFX(context.Weapon, context.Weapon.TryCast<Shotgun>() != null);
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            CancelTracerFX(context.Weapon, context.Weapon.TryCast<ShotgunSynced>() != null);
        }

        private void CancelTracerFX(BulletWeapon weapon, bool isShotgun)
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

        public override IWeaponProperty Clone()
        {
            Projectile copy = new()
            {
                ProjectileType = ProjectileType,
                Speed = Speed,
                AccelScale = AccelScale,
                AccelExponent = AccelExponent,
                AccelTime = AccelTime,
                Gravity = Gravity,
                HitSize = HitSize,
                HitSizeWorld = HitSizeWorld,
                ModelScale = ModelScale,
                EnableTrail = EnableTrail,
                GlowColor = GlowColor,
                GlowRange = GlowRange,
                DamageFriendly = DamageFriendly,
                DamageOwner = DamageOwner,
                VisualLerpDist = VisualLerpDist,
                Lifetime = Lifetime
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteString(nameof(ProjectileType), ProjectileType.ToString());
            writer.WriteNumber(nameof(Speed), Speed);
            writer.WriteNumber(nameof(AccelScale), AccelScale);
            writer.WriteNumber(nameof(AccelExponent), AccelExponent);
            writer.WriteNumber(nameof(AccelTime), AccelTime);
            writer.WriteNumber(nameof(Gravity), Gravity);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteNumber(nameof(HitSizeWorld), HitSizeWorld);
            writer.WriteNumber(nameof(ModelScale), ModelScale);
            writer.WriteBoolean(nameof(EnableTrail), EnableTrail);
            writer.WritePropertyName(nameof(GlowColor));
            JsonSerializer.Serialize(writer, GlowColor, options);
            writer.WriteNumber(nameof(GlowRange), GlowRange);
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
            writer.WriteNumber(nameof(VisualLerpDist), VisualLerpDist);
            writer.WriteNumber(nameof(Lifetime), Lifetime);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (property.ToLowerInvariant())
            {
                case "projectiletype":
                case "type":
                    ProjectileType = reader.GetString().ToEnum(ProjectileType.NotTargetingSmallFast);
                    if (ProjectileType == ProjectileType.GlueFlying || ProjectileType == ProjectileType.GlueLanded)
                        ProjectileType = ProjectileType.NotTargetingSmallFast;
                    break;
                case "speed":
                    Speed = reader.GetSingle();
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
                case "glowcolor":
                case "color":
                    GlowColor = JsonSerializer.Deserialize<Color>(ref reader, options);
                    break;
                case "glowrange":
                case "range":
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
                case "visuallerpdist":
                case "lerpdist":
                    VisualLerpDist = reader.GetSingle();
                    break;
                case "maxlifetime":
                case "lifetime":
                    Lifetime = reader.GetSingle();
                    break;
            }
        }
    }
}
