using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public static class EWCProjectileManager
    {
        internal static readonly Dictionary<ushort, LinkedList<(ushort id, EWCProjectileComponentBase comp)>> PlayerProjectiles = new();

        public static readonly EWCProjectileManagerShooter Shooter = new();

        private static readonly EWCProjectileSyncDestroy _destroySync = new();

        public const float MaxSpeed = 4096; // 2^12
        public const float MaxAccel = 256f;
        public const float MaxAccelExpo = 16f;
        public const float MaxAccelTime = 64f;
        public const float MaxGravity = 1024f; // 2^10
        public const float MaxScale = 1024f;
        public const float MaxGlowRange = 1024f;
        public const float MaxLifetime = 1024f;

        internal static void Init()
        {
            Shooter.Init();
            _destroySync.Setup();
        }

        internal static void Reset()
        {
            Shooter.Reset();
            PlayerProjectiles.Clear();
        }

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
            ProjectileDataDestroy data = new() { id = id };
            _destroySync.Send(data);

            var pair = GetPair(playerIndex, id);
            if (pair == null) return;
            PlayerProjectiles[playerIndex].Remove(pair.Value);
        }

        internal static void Internal_ReceiveProjectileDestroy(ushort playerIndex, ushort id)
        {
            var pair = GetPair(playerIndex, id);
            if (pair == null) return;

            pair.Value.comp.Die();
            PlayerProjectiles[playerIndex].Remove(pair.Value);
        }
    }

    public struct ProjectileDataDestroy
    {
        public ushort playerIndex;
        public ushort id;
    }
}