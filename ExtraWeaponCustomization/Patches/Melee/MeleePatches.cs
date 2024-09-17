﻿using Agents;
using Enemies;
using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.KillTracker;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.Utils;
using Gear;
using HarmonyLib;

namespace ExtraWeaponCustomization.Patches.Melee
{
    [HarmonyPatch]
    internal static class MeleePatches
    {
        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.SetupMeleeAnimations))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(MeleeWeaponFirstPerson __instance)
        {
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
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponWieldContext>.Instance);
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
            CachedCharge = dam == eMeleeWeaponDamage.Heavy ? scale : 0f;
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(MeleeWeaponFirstPerson __instance, MeleeWeaponDamageData data, bool isPush)
        {
            if (isPush) return;

            s_hitData.Setup(__instance, data);
            IDamageable? damageable = s_hitData.damageable;

            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                Agent? agent = damageable?.GetBaseAgent();
                if (agent != null && agent.Type == AgentType.Enemy && agent.Alive)
                    KillTrackerManager.ClearHit(agent.TryCast<EnemyAgent>()!);
                return;
            }

            // Correct damage back to base damage to apply damage mods
            s_hitData.damage = s_origHitDamage;
            s_hitData.precisionMulti = s_origHitPrecision;

            bool backstabDiscard = true;
            WeaponPatches.ApplyEWCHit(cwc, damageable, s_hitData, false, ref s_origHitDamage, ref backstabDiscard);
        }
    }
}