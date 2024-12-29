using EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam;
using HarmonyLib;

namespace EWC.Patches.Enemy
{
    [HarmonyPatch]
    internal static class EnemyFoamPatches
    {
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveGlueDamage))]
        [HarmonyPrefix]
        private static bool FixGlueDamage(Dam_EnemyDamageBase __instance, pMiniDamageData data)
        {
            GlueVolumeDesc glueVolumeDesc = default;
            glueVolumeDesc.volume = data.damage.Get(100f); // Epic .Get(HealthMax) when .Set(100f) is used
            glueVolumeDesc.expandVolume = 0f;
            glueVolumeDesc.currentScale = 0f;
            __instance.AddToTotalGlueVolume(null, glueVolumeDesc);
            return false;
        }

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
