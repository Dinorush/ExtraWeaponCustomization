using Agents;
using Enemies;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public static class EWCProjectileManager
    {
        internal static readonly Dictionary<ushort, LinkedList<(ushort id, EWCProjectileComponentBase comp)>> PlayerProjectiles = new();
        private static readonly Dictionary<ushort, LinkedList<(ushort id, EnemyAgent? enemy, byte limbID)>> _cachedTargets = new();

        public static readonly EWCProjectileManagerShooter Shooter = new();

        private static readonly EWCProjectileSyncDestroy _destroySync = new();
        private static readonly EWCProjectileSyncTarget _targetSync = new();

        internal static void Init()
        {
            Shooter.Init();
            _destroySync.Setup();
            _targetSync.Setup();
        }

        internal static void Reset()
        {
            Shooter.Reset();
            _cachedTargets.Clear();
            PlayerProjectiles.Clear();
        }

        internal static ushort GetNextID(ushort playerIndex)
        {
            if (!PlayerProjectiles.TryGetValue(playerIndex, out var list) || list.Count == 0)
                return 0;

            return (ushort)(PlayerProjectiles[playerIndex].Last!.Value.id + 1);
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
            if (!PlayerProjectiles.ContainsKey(playerIndex))
                PlayerProjectiles[playerIndex] = new();
            PlayerProjectiles[playerIndex].AddLast((id, comp));
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

        public static void DoProjectileTarget(ushort playerIndex, ushort id, EnemyAgent? target, byte limbID)
        {
            ProjectileDataTarget data = new() { playerIndex = playerIndex, id = id, limbID = limbID };
            data.target.Set(target);
            _targetSync.Send(data);
        }

        internal static void Internal_ReceiveProjectileDestroy(ushort playerIndex, ushort id)
        {
            if (!TryGetNode(playerIndex, id, out var node)) return;
            node.Value.comp.Die();
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
}