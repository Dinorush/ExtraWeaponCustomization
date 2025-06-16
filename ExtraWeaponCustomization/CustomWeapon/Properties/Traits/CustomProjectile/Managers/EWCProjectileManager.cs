using Agents;
using Enemies;
using EWC.Attributes;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using SNetwork;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public static class EWCProjectileManager
    {
        internal static readonly Dictionary<ushort, LinkedList<(ushort id, EWCProjectileComponentBase comp)>> PlayerProjectiles = new();
        private static readonly Dictionary<ushort, LinkedList<(ushort id, EnemyAgent? enemy, byte limbID)>> _cachedTargets = new();

        public static readonly EWCProjectileManagerShooter Shooter = new();

        private static readonly EWCProjectileSyncDestroy _destroySync = new();
        private static readonly EWCProjectileSyncTarget _targetSync = new();
        private static readonly EWCProjectileSyncBounce _bounceSync = new();

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _destroySync.Setup();
            _targetSync.Setup();
            _bounceSync.Setup();
        }

        [InvokeOnCleanup]
        private static void Reset()
        {
            Shooter.Reset();
            _cachedTargets.Clear();
            PlayerProjectiles.Clear();
        }

        internal static ushort GetNextID(ushort playerIndex)
        {
            if (!PlayerProjectiles.TryGetValue(playerIndex, out var list) || list.Count == 0)
                return 0;

            return (ushort)(list.Last!.Value.id + 1);
        }

        internal static bool TryGetNode(ushort playerIndex, ushort id, [MaybeNullWhen(false)] out LinkedListNode<(ushort id, EWCProjectileComponentBase comp)> node)
        {
            node = null;
            if (!PlayerProjectiles.TryGetValue(playerIndex, out var list)) return false;

            node = list.First;
            while (node != null && node.Value.id != id)
                node = node.Next;

            return node != null;
        }

        internal static void TryPullCachedTarget(ushort playerIndex, ushort id)
        {
            if (!_cachedTargets.TryGetValue(playerIndex, out var list)) return;

            var node = list.First;
            while (node != null && node.Value.id != id)
                node = node.Next;
            
            if (node == null) return;
            list.Remove(node);
            Internal_ReceiveProjectileTarget(playerIndex, id, node.Value.enemy, node.Value.limbID);
        }

        internal static void AddProjectile(ushort playerIndex, ushort id, EWCProjectileComponentBase comp)
        {
            if (!PlayerProjectiles.TryGetValue(playerIndex, out var list))
                PlayerProjectiles[playerIndex] = list = new();
            list.AddLast((id, comp));
        }

        public static void DoProjectileDestroy(ushort playerIndex, ushort id, bool isLocal)
        {
            if (isLocal)
            {
                ProjectileDataDestroy data = new() { playerIndex = playerIndex, id = id };
                _destroySync.Send(data);
            }

            if (!TryGetNode(playerIndex, id, out var node)) return;
            PlayerProjectiles[playerIndex].Remove(node);
        }

        internal static void Internal_ReceiveProjectileDestroy(ushort playerIndex, ushort id)
        {
            if (!TryGetNode(playerIndex, id, out var node)) return;
            node.Value.comp.Die();
        }

        public static void DoProjectileTarget(ushort playerIndex, ushort id, EnemyAgent? target, byte limbID)
        {
            ProjectileDataTarget data = new() { playerIndex = playerIndex, id = id, limbID = limbID };
            data.target.Set(target);
            _targetSync.Send(data);
        }

        internal static void Internal_ReceiveProjectileTarget(ushort playerIndex, ushort id, EnemyAgent? enemy, byte limbID)
        {
            if (!TryGetNode(playerIndex, id, out var node))
            {
                if (!_cachedTargets.TryGetValue(playerIndex, out var list))
                    _cachedTargets.TryAdd(playerIndex, list = new());
                list.AddLast((id, enemy, limbID));
                return;
            }
            node.Value.comp.Homing.SetHomingAgent(enemy, limbID > 0 && enemy != null ? enemy.Damage.DamageLimbs[limbID] : null);
        }

        public static void DoProjectileBounce(ushort playerIndex, ushort id, Vector3 pos, Vector3 dir)
        {
            ProjectileDataBounce data = new() { playerIndex = playerIndex, id = id, position = pos };
            data.dir.Value = dir;
            _bounceSync.Send(data);
        }

        internal static void Internal_ReceiveProjectileBounce(ushort playerIndex, ushort id, Vector3 pos, Vector3 dir)
        {
            if (!TryGetNode(playerIndex, id, out var node)) return;
            node.Value.comp.SetPosition(pos, dir);
        }
    }

    public struct ProjectileDataDestroy
    {
        public ushort playerIndex;
        public ushort id;
    }

    public struct ProjectileDataTarget
    {
        public ushort playerIndex;
        public ushort id;
        public pEnemyAgent target;
        public byte limbID;
    }

    public struct ProjectileDataBounce
    {
        public ushort playerIndex;
        public ushort id;
        public Vector3 position;
        public LowResVector3_Normalized dir;
    }
}