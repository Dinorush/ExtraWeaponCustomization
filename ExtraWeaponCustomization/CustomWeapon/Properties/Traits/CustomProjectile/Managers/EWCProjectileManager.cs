using Agents;
using Enemies;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public static class EWCProjectileManager
    {
        internal static readonly Dictionary<ushort, LinkedList<(ushort id, EWCProjectileComponentBase comp)>> PlayerProjectiles = new();
        internal static readonly List<Projectile> ProjectileSettings = new();

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
            PlayerProjectiles.Clear();
        }

        internal static ushort RegisterSetting(Projectile setting)
        {
            ProjectileSettings.Add(setting);
            return (ushort) (ProjectileSettings.Count - 1);
        }

        internal static void ClearSettings() => ProjectileSettings.Clear();

        internal static ushort GetNextID(ushort playerIndex)
        {
            if (!PlayerProjectiles.TryGetValue(playerIndex, out var list) || list.Count == 0)
                return 0;

            return (ushort)(PlayerProjectiles[playerIndex].Last!.Value.id + 1);
        }

        internal static (ushort id, EWCProjectileComponentBase comp)? GetPair(ushort playerIndex, ushort id)
        {
            foreach (var pair in PlayerProjectiles[playerIndex])
                if (pair.id == id)
                    return pair;
            return null;
        }

        internal static void AddProjectile(ushort playerIndex, ushort id, EWCProjectileComponentBase comp)
        {
            if (!PlayerProjectiles.ContainsKey(playerIndex))
                PlayerProjectiles[playerIndex] = new();
            PlayerProjectiles[playerIndex].AddLast((id, comp));
        }

        public static void DoProjectileDestroy(ushort playerIndex, ushort id)
        {
            ProjectileDataDestroy data = new() { playerIndex = playerIndex, id = id };
            _destroySync.Send(data);

            var pair = GetPair(playerIndex, id);
            if (pair == null) return;
            PlayerProjectiles[playerIndex].Remove(pair.Value);
        }

        public static void DoProjectileTarget(ushort playerIndex, ushort id, EnemyAgent target, byte limbID)
        {
            ProjectileDataTarget data = new() { playerIndex = playerIndex, id = id, limbID = limbID };
            data.target.Set(target);
            _targetSync.Send(data);
        }

        internal static void Internal_ReceiveProjectileDestroy(ushort playerIndex, ushort id)
        {
            var pair = GetPair(playerIndex, id);
            if (pair == null) return;

            pair.Value.comp.Die();
            PlayerProjectiles[playerIndex].Remove(pair.Value);
        }

        internal static void Internal_ReceiveProjectileTarget(ushort playerIndex, ushort id, EnemyAgent enemy, int limbID)
        {
            var pair = GetPair(playerIndex, id);
            if (pair == null) return;

            pair.Value.comp.Homing.SetHomingAgent(enemy, limbID > 0 ? enemy.Damage.DamageLimbs[limbID] : null);
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