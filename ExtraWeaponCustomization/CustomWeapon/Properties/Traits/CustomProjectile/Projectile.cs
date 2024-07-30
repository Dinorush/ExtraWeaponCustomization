using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components;
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
        IWeaponProperty<WeaponPostFireContext>
    {
        public ProjectileType ProjectileType { get; set; } = ProjectileType.TargetingSmall;
        public float Speed { get; set; } = 0f;
        public float Gravity { get; set; } = 1f;
        public float Size { get; set; } = 0f;
        public float SizeWorld { get; set; } = 0f;

        private CustomWeaponComponent? _cachedCWC;
        private float _weaponRayDist = 100f;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;

        public void Invoke(WeaponPostSetupContext context)
        {
            _weaponRayDist = context.Weapon.MaxRayDist;
            context.Weapon.MaxRayDist = 0f;
        }

        public void Invoke(WeaponClearContext context)
        {
            context.Weapon.MaxRayDist = _weaponRayDist;
        }

        public void Invoke(WeaponPostRayContext context)
        {
            if (!context.Weapon.Owner.IsLocallyOwned && (!SNet.IsMaster || context.Weapon.Owner.Owner.IsBot)) return;

            s_ray.origin = context.Position;
            s_ray.direction = context.Data.fireDir;
            float visualDist = EWCProjectileComponentBase.VisualLerpDist;
            if (Physics.Raycast(s_ray, out s_rayHit, visualDist, LayerManager.MASK_BULLETWEAPON_RAY))
                visualDist = s_rayHit.distance;

            Vector3 position = context.Position + context.Data.fireDir * Math.Min(visualDist, 0.1f);

            var comp = EWCProjectileManager.Shooter.CreateProjectile(context.Weapon.Owner, ProjectileType, position, context.Data.fireDir * Speed, Gravity);
            if (comp == null)
            {
                EWCLogger.Error("Unable to create shooter projectile!");
                return;
            }

            _cachedCWC ??= context.Weapon.GetComponent<CustomWeaponComponent>();
            comp.SetVisualPosition(context.Weapon.MuzzleAlign.position, visualDist);
            comp.Hitbox.Init(_cachedCWC, this);
        }

        // Cancel tracer FX
        public void Invoke(WeaponPostFireContext context)
        {
            if (!context.Weapon.Owner.IsLocallyOwned && (!SNet.IsMaster || context.Weapon.Owner.Owner.IsBot)) return;

            int shots = 1;
            if (context.Weapon.TryCast<Shotgun>() != null)
                shots = context.Weapon.ArchetypeData.ShotgunBulletCount;

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
                Gravity = Gravity,
                Size = Size,
                SizeWorld = SizeWorld
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteString(nameof(ProjectileType), ProjectileType.ToString());
            writer.WriteNumber(nameof(Speed), Speed);
            writer.WriteNumber(nameof(Gravity), Gravity);
            writer.WriteNumber(nameof(Size), Size);
            writer.WriteNumber(nameof(SizeWorld), SizeWorld);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (property.ToLowerInvariant())
            {
                case "projectiletype":
                case "type":
                    ProjectileType = reader.GetString().ToEnum(ProjectileType.TargetingMedium);
                    break;
                case "speed":
                    Speed = reader.GetSingle();
                    break;
                case "gravity":
                    Gravity = reader.GetSingle();
                    break;
                case "size":
                    Size = reader.GetSingle();
                    break;
                case "sizeworld":
                    SizeWorld = reader.GetSingle();
                    break;
            }
        }
    }
}
