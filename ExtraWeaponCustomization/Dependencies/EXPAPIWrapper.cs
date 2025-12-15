using BepInEx.Unity.IL2CPP;
using EndskApi.Api;
using EndskApi.Enums.EnemyKill;
using EndskApi.Information.EnemyKill;
using GTFuckingXP.Extensions;
using Enemies;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using GTFuckingXP.Enums;
using GTFuckingXP.Information.Level;
using System.Runtime.CompilerServices;
using EWC.CustomWeapon.Enums;

namespace EWC.Dependencies
{
    internal static class EXPAPIWrapper
    {
        public const string PLUGIN_GUID = "Endskill.GTFuckingXP";
        // Need to work with existing cache since we need to add mix the existing behavior with DoT and Explosive damage
        private const string CacheKey = "EndskApi";
        private const string EnemyKillKey = "EnemyKillCallbacks";

        public readonly static bool HasEXP;

        static EXPAPIWrapper()
        {
            HasEXP = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static float GetAmmoMod(bool local) => local && HasEXP ? EXPGetAmmoMod() : 1f;

        public static float GetExplosionResistanceMod(PlayerAgent player) => HasEXP ? EXPGetExplosionResistanceMod(player) : 1f;

        public static float GetDamageMod(bool local, WeaponType type) => local && HasEXP && PlayerManager.HasLocalPlayerAgent() ? EXPGetDamageMod(type) : 1f;

        public static float GetHealthRegenMod(PlayerAgent player) => HasEXP ? EXPGetHealthRegenMod(player) : 1f;

        public static void RegisterDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill)
        {
            if (HasEXP)
                EXPDidDamage(enemy, source, damage, willKill);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float EXPGetAmmoMod() => CacheApiWrapper.GetActiveLevel().CustomScaling.FirstOrDefault(buff => buff.CustomBuff == CustomScaling.AmmoEfficiency)?.Value ?? 1f;

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
            Level level;
            if (player.IsLocallyOwned)
                level = CacheApiWrapper.GetActiveLevel();
            else if (!CacheApiWrapper.GetPlayerToLevelMapping().TryGetValue(player.PlayerSlotIndex, out level!))
                return 1f;

            CustomScalingBuff? buff = level.CustomScaling.FirstOrDefault(buff => buff.CustomBuff == CustomScaling.ExplosionResistance);
            return buff != null ? 2f - buff.Value : 1f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float EXPGetHealthRegenMod(PlayerAgent player)
        {
            Level level;
            if (player.IsLocallyOwned)
                level = CacheApiWrapper.GetActiveLevel();
            else if (!CacheApiWrapper.GetPlayerToLevelMapping().TryGetValue(player.PlayerSlotIndex, out level!))
                return 1f;

            CustomScalingBuff? buff = level.CustomScaling.FirstOrDefault(buff => buff.CustomBuff == CustomScaling.RegenStartDelayMultiplier);
            return buff != null ? buff.Value : 1f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EXPDidDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill) => EnemyKillApi.RegisterDamage(enemy, source, damage, willKill);
    }
}
