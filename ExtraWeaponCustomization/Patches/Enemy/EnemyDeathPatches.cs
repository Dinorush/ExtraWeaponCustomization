using Enemies;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using HarmonyLib;
using System;
using EWC.CustomWeapon.ObjectWrappers;

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

            var killInfo = KillTrackerManager.GetKillHitContexts(__instance.m_ai.m_enemyAgent);
            if (killInfo == null) return;

            float maxTime = 0f;
            ObjectWrapper<CustomWeaponComponent>? lastCWC = null;
            foreach ((var cwc, (var context, float time)) in killInfo)
            {
                if (time > maxTime)
                {
                    lastCWC = cwc;
                    maxTime = time;
                }
            }
            if (lastCWC == null) return;

            foreach ((var cwc, (var context, float time)) in killInfo)
                cwc.Object!.Invoke(new WeaponPostKillContext(context, time, cwc.Pointer == lastCWC.Pointer));
        }
    }
}
