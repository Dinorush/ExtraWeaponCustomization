using EWC.CustomWeapon.HitTracker;
using HarmonyLib;
using System;

namespace EWC.Patches.Enemy
{
    [HarmonyPatch]
    internal static class EnemyHitreactPatches
    {
        [HarmonyPatch(typeof(ES_Hitreact), nameof(ES_Hitreact.DoHitReact))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_DoHitreact(ES_HitreactBase __instance, ES_HitreactType hitreactType)
        {
            if (hitreactType != ES_HitreactType.Light && hitreactType != ES_HitreactType.Heavy) return;

            HitTrackerManager.RunStaggerContexts(__instance.m_enemyAgent, hitreactType == ES_HitreactType.Heavy);
        }

        [HarmonyPatch(typeof(ES_HitreactFlyer), nameof(ES_HitreactFlyer.DoHitReact), new Type[] { typeof(int), typeof(ES_HitreactType), typeof(ImpactDirection), typeof(float), typeof(bool), typeof(UnityEngine.Vector3), typeof(UnityEngine.Vector3) })]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_DoHitreactFlyer(ES_HitreactBase __instance, ES_HitreactType reactionType)
        {
            if (reactionType != ES_HitreactType.Light && reactionType != ES_HitreactType.Heavy) return;

            HitTrackerManager.RunStaggerContexts(__instance.m_enemyAgent, reactionType == ES_HitreactType.Heavy);
        }
    }
}
