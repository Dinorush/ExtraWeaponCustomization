using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public static class EWCProjectileManager
    {
        internal static readonly LinkedList<(ushort id, EWCProjectileComponentBase comp)> PlayerProjectiles = new();

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

        internal static ushort GetNextID()
        {
            if (PlayerProjectiles.Count == 0)
                return 0;

            return (ushort)(PlayerProjectiles.Last!.Value.id + 1);
        }

        internal static (ushort id, EWCProjectileComponentBase comp)? GetPair(ushort id)
        {
            foreach (var pair in PlayerProjectiles)
                if (pair.id == id)
                    return pair;
            return null;
        }

        public static void DoProjectileDestroy(ushort id)
        {
            ProjectileDataDestroy data = new() { id = id };
            _destroySync.Send(data);

            var pair = GetPair(id);
            if (pair == null) return;
            PlayerProjectiles.Remove(pair.Value);
        }

        internal static void Internal_ReceiveProjectileDestroy(ushort id)
        {
            var pair = GetPair(id);
            if (pair == null) return;

            pair.Value.comp.Die();
            PlayerProjectiles.Remove(pair.Value);
        }
    }

    public struct ProjectileDataDestroy
    {
        public ushort id;
    }
}