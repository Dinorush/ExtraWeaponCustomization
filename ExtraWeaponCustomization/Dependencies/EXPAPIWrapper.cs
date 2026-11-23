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
using XpExpansions.Manager;
using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Attributes;

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

        [InvokeOnLoad]
        private static void Init()
        {
            if (HasEXP)
                EXPAddCallbacks();
        }

        public static float GetCapacityMod(IOwnerComp owner) => HasEXP ? EXPGetCapacityMod(owner) : 1f;

        public static float GetExplosionResistanceMod(PlayerAgent player) => HasEXP ? EXPGetExplosionResistanceMod(player) : 1f;

        public static float GetLocalDamageMod()
        {
            if (!HasEXP)
                return 1f;
            else
                return EXPGetLocalDamageMod();
        }

        public static bool GetStatMods(CustomWeaponComponent cwc, out float damage, out float stagger, out float backstab)
        {
            if (!HasEXP || !cwc.Owner.IsType(OwnerType.Local))
            {
                damage = 1f;
                stagger = 1f;
                backstab = 1f;
                return false;
            }
            else
                return EXPGetStatMods(cwc.Weapon.Type, out damage, out stagger, out backstab);
        }

        public static float GetHealthRegenMod(PlayerAgent player) => HasEXP ? EXPGetHealthRegenMod(player) : 1f;

        public static void DoLifesteal(CustomWeaponComponent cwc, float damage)
        {
            if (HasEXP && cwc.Owner.IsType(OwnerType.Local) && PlayerManager.HasLocalPlayerAgent())
                EXPDoLifesteal(cwc.Owner, cwc.Weapon.Type, damage);
        }

        public static void RegisterDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill)
        {
            if (HasEXP)
                EXPDidDamage(enemy, source, damage, willKill);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EXPAddCallbacks()
        {
            ToolAbilityManager.OnSentryPickup += (item) =>
            {
                var cwc = item.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponSentryStateContext(false));
            };
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
        private static float EXPGetLocalDamageMod() => CacheApiWrapper.GetActiveLevel().WeaponDamageMultiplier;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool EXPGetStatMods(WeaponType type, out float damage, out float stagger, out float backstab)
        {
            var level = CacheApiWrapper.GetActiveLevel();
            if (type.HasFlag(WeaponType.BulletWeapon))
            {
                damage = level.WeaponDamageMultiplier;
                stagger = level.CustomScaling.GetValueOrDefault(CustomScaling.WeaponStaggerMultiplier, 1f);
                backstab = level.CustomScaling.GetValueOrDefault(CustomScaling.WeaponBackstabMultiplier, 1f);
            }
            else if (type.HasFlag(WeaponType.Melee))
            {
                damage = level.MeleeDamageMultiplier;
                stagger = level.CustomScaling.GetValueOrDefault(CustomScaling.MeleeStaggerMultiplier, 1f);
                backstab = level.CustomScaling.GetValueOrDefault(CustomScaling.MeleeBackstabMultiplier, 1f);
            }
            else
            {
                damage = 1f;
                stagger = 1f;
                backstab = 1f;
                return false;
            }
            return true;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EXPDoLifesteal(IOwnerComp owner, WeaponType type, float damage)
        {
            float lifesteal = 0f;
            if (type.HasFlag(WeaponType.BulletWeapon))
                lifesteal = CacheApiWrapper.GetActiveLevel().CustomScaling.GetValueOrDefault(CustomScaling.WeaponLifesteal);
            else if (type.HasFlag(WeaponType.Melee))
                lifesteal = CacheApiWrapper.GetActiveLevel().CustomScaling.GetValueOrDefault(CustomScaling.MeleeLifesteal);
            if (lifesteal == 0f) { return; }

            var playerAgent = owner.Player!;
            playerAgent.Damage.AddHealth(damage * lifesteal * playerAgent.Damage.HealthMax / 100f, playerAgent);
        }
    }
}
