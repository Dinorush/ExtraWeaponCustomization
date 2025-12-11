using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using UnityEngine;
using System.Collections.Generic;
using SNetwork;
using EWC.Utils;
using System;
using EWC.Attributes;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public class EWCProjectileManagerShooter
    {
        private readonly Dictionary<ProjectileType, Queue<EWCProjectileComponentShooter>> _pools = new();
        private static readonly EWCProjectileSyncShooter s_shooterSync = new();

        [InvokeOnAssetLoad]
        private static void Init()
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

        public EWCProjectileComponentShooter CreateAndSendProjectile(Projectile projBase, ushort shotIndex, Vector3 position, Vector3 fxPos, HitData hitData, IntPtr ignoreEnt = default)
        {
            ushort index = (ushort) (projBase.CWC.Owner.Player?.PlayerSlotIndex ?? 0);
            ProjectileDataShooter data = new()
            {
                playerIndex = index,
                shotIndex = shotIndex,
                id = EWCProjectileManager.GetNextID(index),
                propertyID = projBase.SyncPropertyID,
                position = position
            };
            data.dir.Value = hitData.fireDir;
            data.localFXPos.Set(fxPos - position, 10f);

            s_shooterSync.Send(data);
            EWCProjectileComponentShooter comp = GetFromPool(projBase.ProjectileType);
            EWCProjectileManager.AddProjectile(index, data.id, comp);
            comp.Init(index, shotIndex, data.id, projBase, true, position, hitData.fireDir, hitData, ignoreEnt);
            return comp;
        }

        internal void Internal_ReceiveProjectile(ushort index, ushort shotIndex, ushort id, Projectile projBase, Vector3 position, Vector3 fxPos, Vector3 dir)
        {
            EWCProjectileComponentShooter comp = GetFromPool(projBase.ProjectileType);
            EWCProjectileManager.AddProjectile(index, id, comp);
            comp.Init(index, shotIndex, id, projBase, false, position, dir);
            if (projBase.VisualLerpDist > 0 && fxPos != Vector3.zero)
                comp.SetVisualPosition(fxPos, projBase.VisualLerpDist);
            EWCProjectileManager.TryPullCachedTarget(index, id);
        }
    }

    public struct ProjectileDataShooter
    {
        public ushort playerIndex;
        public ushort shotIndex;
        public ushort id;
        public ushort propertyID;
        public Vector3 position;
        public LowResVector3 localFXPos;
        public LowResVector3_Normalized dir;
    }
}
