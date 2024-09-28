using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using UnityEngine;
using System.Collections.Generic;
using SNetwork;
using EWC.Networking.Structs;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public class EWCProjectileManagerShooter
    {
        private readonly Dictionary<ProjectileType, Queue<EWCProjectileComponentShooter>> _pools = new();
        private static readonly EWCProjectileSyncShooter s_shooterSync = new();

        public void Init()
        {
            s_shooterSync.Setup();
        }

        public void Reset()
        {
            foreach (var queue in _pools.Values)
                foreach (var comp in queue)
                    GameObject.Destroy(comp.gameObject);

            _pools.Clear();
        }

        public void ReturnToPool(EWCProjectileComponentShooter projectile)
        {
            _pools[projectile.Type].Enqueue(projectile);
        }

        public EWCProjectileComponentShooter GetFromPool(ProjectileType type)
        {
            if (!_pools.ContainsKey(type))
                _pools.Add(type, new Queue<EWCProjectileComponentShooter>());

            Queue<EWCProjectileComponentShooter> pool = _pools[type];
            if (pool.TryDequeue(out EWCProjectileComponentShooter? comp)) return comp;

            GameObject go = ProjectileManager.SpawnProjectileType(type, Vector3.zero, Quaternion.identity);
            comp = go.AddComponent<EWCProjectileComponentShooter>();
            comp.Type = type;

            return comp;
        }

        public EWCProjectileComponentShooter? CreateAndSendProjectile(Projectile projBase, Vector3 position, Vector3 dir)
        {
            ProjectileDataShooter data = new()
            {
                id = EWCProjectileManager.GetNextID(),
                type = projBase.ProjectileType,
                position = position,
                trail = projBase.EnableTrail,
                glowColor = projBase.GlowColor,
            };
            data.dir.Value = dir;
            data.speed.Set(projBase.Speed, EWCProjectileManager.MaxSpeed);
            data.accel.Set(projBase.AccelScale, EWCProjectileManager.MaxSpeed);
            data.accelExpo.Set(projBase.AccelExponent, EWCProjectileManager.MaxAccelExpo);
            data.accelTime.Set(projBase.AccelTime, EWCProjectileManager.MaxAccelTime);
            data.gravity.Set(projBase.Gravity, EWCProjectileManager.MaxGravity);
            data.scale.Set(projBase.ModelScale, EWCProjectileManager.MaxScale);
            data.glowRange.Set(projBase.GlowRange, EWCProjectileManager.MaxGlowRange);
            data.lifetime.Set(projBase.Lifetime, EWCProjectileManager.MaxLifetime);

            s_shooterSync.Send(data);
            EWCProjectileComponentShooter comp = GetFromPool(data.type);
            EWCProjectileManager.PlayerProjectiles.AddLast((data.id, comp));
            comp.Init(data.id, position, dir * projBase.Speed, projBase.AccelScale, projBase.AccelExponent, projBase.AccelTime, 
                projBase.Gravity, projBase.ModelScale, projBase.EnableTrail, projBase.GlowColor, projBase.GlowRange, projBase.Lifetime, true);
            return comp;
        }

        internal void Internal_ReceiveProjectile(ushort id, ProjectileType type, Vector3 position, Vector3 velocity, float accel, float accelExpo, float accelTime, float gravity, float scale, bool trail, Color glowColor, float glowRange, float lifetime)
        {
            EWCProjectileComponentShooter comp = GetFromPool(type);
            EWCProjectileManager.PlayerProjectiles.AddLast((id, comp));
            comp.Init(id, position, velocity, accel, accelExpo, accelTime, gravity, scale, trail, glowColor, glowRange, lifetime, false);
        }
    }

    public struct ProjectileDataShooter
    {
        public ushort id;
        public ProjectileType type;
        public Vector3 position;
        public LowResVector3_Normalized dir;
        public UFloat16 speed;
        public UFloat16 accel;
        public UFloat16 accelExpo;
        public UFloat16 accelTime;
        public UFloat16 gravity;
        public UFloat16 scale;
        public bool trail;
        public LowResColor glowColor;
        public SFloat16 glowRange;
        public UFloat16 lifetime;
    }
}
