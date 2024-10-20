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

        public static float GetAmmoMod()
        {
            return HasEXP ? EXPGetAmmoMod() : 1f;
        }

        public static void ApplyMod(ref float damage)
        {
            if (HasEXP)
                EXPDamageMod(ref damage);
        }

        public static void RegisterDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill)
        {
            if (HasEXP)
                EXPDidDamage(enemy, source, damage, willKill);
        }

        private static float EXPGetAmmoMod() => CacheApiWrapper.GetActiveLevel().CustomScaling.FirstOrDefault(buff => buff.CustomBuff == CustomScaling.AmmoEfficiency)?.Value ?? 1f;

        private static void EXPDamageMod(ref float damage) => damage *= CacheApiWrapper.GetActiveLevel().WeaponDamageMultiplier;

        private static void EXPDidDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill)
        {
            if (source == null) return;

            var damageDistribution = CacheApi.GetInstance<Dictionary<IntPtr, EnemyKillDistribution>>(CacheKey);

            if (!damageDistribution.TryGetValue(enemy.Pointer, out EnemyKillDistribution? distribution))
            {
                distribution = new EnemyKillDistribution(enemy);
                damageDistribution[enemy.Pointer] = distribution;
            }

            distribution.AddDamageDealtByPlayerAgent(source, damage);

            if (willKill)
            {
                distribution.LastHitDealtBy = source;
                distribution.lastHitType = LastHitType.ShootyWeapon;

                if (CacheApi.TryGetInformation<List<Action<EnemyKillDistribution>>>(EnemyKillKey, out var callBackList, CacheKey, false))
                {
                    foreach (var callBack in callBackList)
                        callBack.Invoke(distribution);
                }
            }
        }
    }
}
