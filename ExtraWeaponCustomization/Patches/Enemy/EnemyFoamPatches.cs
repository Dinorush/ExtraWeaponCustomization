using EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam;
using HarmonyLib;

namespace EWC.Patches.Enemy
{
    [HarmonyPatch]
    internal static class EnemyFoamPatches
    {
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.AddToTotalGlueVolume))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SyncWithFoamManager(Dam_EnemyDamageBase __instance, GlueGunProjectile? proj, GlueVolumeDesc volume)
        {
            float mod = proj != null ? proj.EffectMultiplier : 1f;
            FoamManager.AddFoam(__instance, (volume.volume + volume.expandVolume) * mod, FoamManager.GetProjProperty(proj));
        }
    }
}
