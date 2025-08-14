using Enemies;
using EWC.CustomWeapon.HitTracker;
using HarmonyLib;
using System;

namespace EWC.Patches.Enemy
{
    [HarmonyPatch]
    internal static class EnemyDeathPatches
    {
        [HarmonyPatch(typeof(EnemyBehaviour), nameof(EnemyBehaviour.ChangeState), new Type[] { typeof(EB_States) })]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Death(EnemyBehaviour __instance, EB_States state)
        {
            if (__instance.m_currentStateName == state || state != EB_States.Dead) return;

            HitTrackerManager.RunKillContexts(__instance.m_ai.m_enemyAgent);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.ShowHitIndicator))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_DeathHitmarker(Dam_EnemyDamageLimb __instance, bool willDie)
        {
            if (!willDie) return;

            HitTrackerManager.RunKillContexts(__instance.m_base.Owner);
        }
    }
}
