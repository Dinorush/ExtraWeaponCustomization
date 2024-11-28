using Agents;
using Enemies;
using EWC.CustomWeapon;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext;
using EWC.Utils;
using Gear;
using HarmonyLib;
using System;

namespace EWC.Patches.Melee
{
    [HarmonyPatch]
    internal static class MeleePatches
    {
        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.SetupMeleeAnimations))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(MeleeWeaponFirstPerson __instance)
        {
            CachedCharge = 0f;
            CustomWeaponManager.Current.AddWeaponListener(__instance);
            CustomWeaponData? data = CustomWeaponManager.Current.GetCustomMeleeData(__instance.MeleeArchetypeData.persistentID);
            if (data == null) return;

            if (__instance.gameObject.GetComponent<CustomWeaponComponent>() != null) return;

            CustomWeaponComponent cwc = __instance.gameObject.AddComponent<CustomWeaponComponent>();
            cwc.Register(data);
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.OnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateCurrentWeapon(MeleeWeaponFirstPerson __instance)
        {
            CachedCharge = 0f;
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponWieldContext>.Instance);
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.OnUnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ClearCharge(MeleeWeaponFirstPerson __instance)
        {
            CachedCharge = 0f;
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponUnWieldContext>.Instance);
        }

        private readonly static HitData s_hitData = new();
        private static float s_origHitDamage = 0f;
        private static float s_origHitPrecision = 0f;
        public static float CachedCharge { get; private set; } = 0f;

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.SetNextDamageToDeal))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetDamageCallback(MeleeWeaponFirstPerson __instance, eMeleeWeaponDamage dam, float scale)
        {
            s_origHitDamage = __instance.m_damageToDeal;
            s_origHitPrecision = __instance.m_precisionMultiToDeal;
            CachedCharge = dam == eMeleeWeaponDamage.Heavy ? (float) Math.Cbrt(scale) : 0f;
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(MeleeWeaponFirstPerson __instance, MeleeWeaponDamageData data, bool isPush)
        {
            if (isPush) return;

            s_hitData.Setup(__instance, data);
            IDamageable? damageable = s_hitData.damageable;
            IDamageable? baseDamageable = damageable?.GetBaseDamagable();
            if (baseDamageable != null && baseDamageable.GetHealthRel() <= 0) return;

            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                Agent? agent = damageable?.GetBaseAgent();
                if (agent != null && agent.Type == AgentType.Enemy && agent.Alive)
                    KillTrackerManager.ClearHit(agent.TryCast<EnemyAgent>()!);
                WeaponPatches.CachedHitCC = null;
                return;
            }

            // Correct damage back to base damage to apply damage mods
            s_hitData.damage = s_origHitDamage;
            s_hitData.precisionMulti = s_origHitPrecision;

            WeaponPatches.ApplyEWCHit(cwc, s_hitData, false, ref s_origHitDamage, out _);
        }
    }
}
