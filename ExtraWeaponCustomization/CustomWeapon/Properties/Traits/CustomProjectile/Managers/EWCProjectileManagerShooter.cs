using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using UnityEngine;
using Player;
using System.Collections.Generic;
using SNetwork;
using ExtraWeaponCustomization.Utils;

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
            foreach(var queue in _pools.Values)
                foreach(var comp in queue)
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

        public EWCProjectileComponentShooter? CreateProjectile(PlayerAgent source, ProjectileType type, Vector3 position, Vector3 velocity, float gravity)
        {
            int characterIndex = source.Owner.CharacterIndex;
            if (characterIndex < 0) return null;

            ProjectileDataShooter data = new()
            {
                characterIndex = (ushort)characterIndex,
                id = EWCProjectileManager.GetNextID(characterIndex),
                type = type,
                position = position,
                velocity = velocity
            };
            data.gravity.Set(gravity, EWCProjectileManager.MaxGravity);

            s_shooterSync.Send(data);
            EWCProjectileComponentShooter comp = GetFromPool(type);
            EWCProjectileManager.GetProjectileList(characterIndex).AddLast((data.id, comp));
            comp.Init(characterIndex, data.id, position, velocity, gravity);
            return comp;
        }

        internal void Internal_ReceiveProjectile(int characterIndex, ushort id, ProjectileType type, Vector3 position, Vector3 velocity, float gravity)
        {
            EWCProjectileComponentShooter comp = GetFromPool(type);
            EWCProjectileManager.GetProjectileList(characterIndex).AddLast((id, comp));
            comp.Init(-1, id, position, velocity, gravity);
        }
    }

    public struct ProjectileDataShooter
    {
        public ushort characterIndex;
        public ushort id;
        public ProjectileType type;
        public Vector3 position;
        public Vector3 velocity;
        public UFloat16 gravity;
    }
}
