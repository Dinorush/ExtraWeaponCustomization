using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using System.Collections.Generic;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    public static class EWCProjectileManager
    {
        private static readonly List<LinkedList<(ushort id, EWCProjectileComponentBase comp)>> _playerProjectiles = new();

        public static readonly EWCProjectileManagerShooter Shooter = new();

        private static readonly EWCProjectileSyncDestroy _destroySync = new();

        public const float MaxGravity = 1024f; // 2^10
        public static int MaskEntityAndWorld;
        public static int MaskEntity;
        public static int MaskWorld;

        internal static void Init()
        {
            Shooter.Init();
            _destroySync.Setup();
            MaskEntity = LayerMask.GetMask("PlayerMover", "PlayerSynced", "EnemyDamagable");
            MaskWorld = LayerMask.GetMask("Default", "Default_NoGraph", "Default_BlockGraph", "ProjectileBlocker", "Dynamic");
            MaskEntityAndWorld = MaskEntity | MaskWorld;
        }

        internal static void Reset()
        {
            Shooter.Reset();
            _playerProjectiles.Clear();
        }

        internal static LinkedList<(ushort id, EWCProjectileComponentBase comp)> GetProjectileList(int characterIndex)
        {
            _playerProjectiles.EnsureCapacity(characterIndex + 1);
            while (_playerProjectiles.Count <= characterIndex)
                _playerProjectiles.Add(new());
            return _playerProjectiles[characterIndex];
        }

        internal static ushort GetNextID(int characterIndex)
        {
            var list = GetProjectileList(characterIndex);
            if (list.Count == 0)
                return 0;

            return (ushort)(list.Last!.Value.id + 1);
        }

        internal static (ushort id, EWCProjectileComponentBase? comp) GetPair(int characterIndex, ushort id)
        {
            foreach (var pair in GetProjectileList(characterIndex))
                if (pair.id == id)
                    return pair;
            return (0, null);
        }

        public static void DoProjectileDestroy(int characterIndex, ushort id)
        {
            if (characterIndex < 0) return;

            ProjectileDataDestroy data = new() { characterIndex = (ushort)characterIndex, id = id };
            _destroySync.Send(data);

            var pair = GetPair(characterIndex, id);
            if (pair.comp == null) return;
            GetProjectileList(characterIndex).Remove(pair!);
        }

        internal static void Internal_ReceiveProjectileDestroy(int characterIndex, ushort id)
        {
            var pair = GetPair(characterIndex, id);
            if (pair.comp == null) return;

            pair.comp.Die();
            GetProjectileList(characterIndex).Remove(pair!);
        }
    }

    public struct ProjectileDataDestroy
    {
        public ushort characterIndex;
        public ushort id;
    }
}