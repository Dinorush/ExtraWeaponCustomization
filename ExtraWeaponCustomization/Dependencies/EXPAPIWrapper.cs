using BepInEx.Unity.IL2CPP;
using EndskApi.Api;
using GTFuckingXP.Extensions;
using Enemies;
using Player;
using GTFuckingXP.Enums;
using System.Runtime.CompilerServices;
using EWC.CustomWeapon.Enums;
using System.Collections.Generic;
using EWC.CustomWeapon.ComponentWrapper;

namespace EWC.Dependencies
{
    internal static class EXPAPIWrapper
    {
        public const string PLUGIN_GUID = "Endskill.GTFuckingXP";

        public readonly static bool HasEXP;

        static EXPAPIWrapper()
        {
            HasEXP = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static float GetCapacityMod(IOwnerComp owner) => HasEXP ? EXPGetCapacityMod(owner) : 1f;

        public static float GetExplosionResistanceMod(PlayerAgent player) => HasEXP ? EXPGetExplosionResistanceMod(player) : 1f;

        public static float GetDamageMod(bool local, WeaponType type) => local && HasEXP && PlayerManager.HasLocalPlayerAgent() ? EXPGetDamageMod(type) : 1f;

        public static float GetHealthRegenMod(PlayerAgent player) => HasEXP ? EXPGetHealthRegenMod(player) : 1f;

        public static void RegisterDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill)
        {
            if (HasEXP)
                EXPDidDamage(enemy, source, damage, willKill);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float EXPGetCapacityMod(IOwnerComp owner)
        {
            if (!CacheApiWrapper.TryGetActiveLevel(owner.Player, out var level)) return 1f;
            var effEnum = owner.IsType(OwnerType.Sentry) ? CustomScaling.ToolEfficiency : CustomScaling.AmmoEfficiency;
            var capEnum = owner.IsType(OwnerType.Sentry) ? CustomScaling.ToolCapacity : CustomScaling.AmmoCapacity;
            var scaling = level.CustomScaling;
            float mod = scaling.GetValueOrDefault(effEnum, 1f);
            mod *= scaling.GetValueOrDefault(capEnum, 1f);
            return mod;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float EXPGetDamageMod(WeaponType type)
        {
            if (type.HasFlag(WeaponType.BulletWeapon))
                return CacheApiWrapper.GetActiveLevel().WeaponDamageMultiplier;
            else if (type.HasFlag(WeaponType.Melee))
                return CacheApiWrapper.GetActiveLevel().MeleeDamageMultiplier;
            return 1f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float EXPGetExplosionResistanceMod(PlayerAgent player)
        {
            if (!CacheApiWrapper.TryGetActiveLevel(player, out var level)) return 1f;

            return level.CustomScaling.TryGetValue(CustomScaling.ExplosionResistance, out var value) ? 2f - value : 1f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float EXPGetHealthRegenMod(PlayerAgent player)
        {
            if (!CacheApiWrapper.TryGetActiveLevel(player, out var level)) return 1f;

            return level.CustomScaling.TryGetValue(CustomScaling.RegenStartDelayMultiplier, out var value) ? value : 1f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EXPDidDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill) => EnemyKillApi.RegisterDamage(enemy, source, damage, willKill);
    }
}
