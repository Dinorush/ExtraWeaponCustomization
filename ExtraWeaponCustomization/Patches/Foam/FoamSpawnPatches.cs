using EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam;
using HarmonyLib;

namespace EWC.Patches.Enemy
{
    [HarmonyPatch]
    internal static class FoamSpawnPatches
    {
        // When a bubble has more than enough amount to foam an enemy, a new one is spawned with ID | 0x8000u on the enemy.
        private const uint VanillaEnemyMergeIDMod = 0x8000u;

        [HarmonyPatch(typeof(ProjectileManager), nameof(ProjectileManager.SpawnGlueGunProjectileIfNeeded))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void CachePropertyOnEnemySplitFoam(ProjectileManager __instance, uint syncID, GlueGunProjectile __result)
        {
            if ((syncID & VanillaEnemyMergeIDMod) == 0) return;

            var parent = __instance.m_glueGunProjectiles[syncID & ~VanillaEnemyMergeIDMod];
            var property = FoamManager.GetProjProperty(parent);
            if (property != null)
                FoamManager.AddFoamBubble(__result, property);

            // Fix missing assignments
            __result.m_glueStrengthMultiplier = parent.m_glueStrengthMultiplier;
            __result.m_owner = parent.m_owner;
        }

        // The mod is 32768 which can reasonably be reached with crazy weapons, so need a fix.
        [HarmonyPatch(typeof(ProjectileManager), nameof(ProjectileManager.GetNextSyncID))]
        [HarmonyPriority(Priority.High)]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void FixSyncIDOverflow(ProjectileManager __instance, ref uint __result)
        {
            if ((__result & VanillaEnemyMergeIDMod) > 0)
                __instance.m_nextID += VanillaEnemyMergeIDMod;
        }
    }
}
