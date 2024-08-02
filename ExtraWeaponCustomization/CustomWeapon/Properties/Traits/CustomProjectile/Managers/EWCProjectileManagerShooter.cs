using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using UnityEngine;
using System.Collections.Generic;
using SNetwork;
using ExtraWeaponCustomization.Networking.Structs;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers
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

        public EWCProjectileComponentShooter? CreateAndSendProjectile(ProjectileType type, Vector3 position, Vector3 velocity, float gravity, float scale, Color glowColor, float glowRange)
        {
            ProjectileDataShooter data = new()
            {
                id = EWCProjectileManager.GetNextID(),
                type = type,
                position = position,
                glowColor = glowColor
            };
            data.dir.Value = velocity.normalized;
            data.speed.Set(velocity.magnitude, EWCProjectileManager.MaxSpeed);
            data.gravity.Set(gravity, EWCProjectileManager.MaxGravity);
            data.scale.Set(scale, EWCProjectileManager.MaxScale);
            data.glowRange.Set(glowRange, EWCProjectileManager.MaxGlowRange);

            s_shooterSync.Send(data);
            EWCProjectileComponentShooter comp = GetFromPool(type);
            EWCProjectileManager.PlayerProjectiles.AddLast((data.id, comp));
            comp.Init(data.id, position, velocity, gravity, scale, glowColor, glowRange, true);
            return comp;
        }

        internal void Internal_ReceiveProjectile(ushort id, ProjectileType type, Vector3 position, Vector3 velocity, float gravity, float scale, Color glowColor, float glowRange)
        {
            EWCProjectileComponentShooter comp = GetFromPool(type);
            EWCProjectileManager.PlayerProjectiles.AddLast((id, comp));
            comp.Init(id, position, velocity, gravity, scale, glowColor, glowRange, false);
        }
    }

    public struct ProjectileDataShooter
    {
        public ushort id;
        public ProjectileType type;
        public Vector3 position;
        public LowResVector3_Normalized dir;
        public UFloat16 speed;
        public UFloat16 gravity;
        public UFloat16 scale;
        public LowResColor glowColor;
        public SFloat16 glowRange;
    }
}
