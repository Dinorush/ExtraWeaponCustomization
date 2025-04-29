using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext;
using EWC.Utils;
using Gear;
using HarmonyLib;
using System;
using EWC.CustomWeapon.CustomShot;

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
            if (!CustomWeaponManager.TryGetCustomMeleeData(__instance.MeleeArchetypeData.persistentID, out var data)) return;

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

        public readonly static HitData HitData = new(CustomWeapon.Enums.DamageType.Bullet);
        private static CustomWeaponComponent? _cachedCWC = null;
        public static float CachedCharge { get; private set; } = 0f;

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.SetNextDamageToDeal))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetDamageCallback(MeleeWeaponFirstPerson __instance, eMeleeWeaponDamage dam, float scale)
        {
            _cachedCWC = __instance.GetComponent<CustomWeaponComponent>();
            if (_cachedCWC == null) return;
            ShotManager.AdvanceGroupMod(_cachedCWC);

            HitData.shotInfo.Reset(__instance.m_damageToDeal, __instance.m_precisionMultiToDeal, __instance.m_staggerMultiToDeal, false);
            HitData.shotInfo.NewShot(_cachedCWC);
            CachedCharge = dam == eMeleeWeaponDamage.Heavy ? (float)Math.Cbrt(scale) : 0f;
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(MeleeWeaponFirstPerson __instance, MeleeWeaponDamageData data, bool isPush)
        {
            if (isPush || _cachedCWC == null) return;

            HitData.Setup(__instance, data);
            IDamageable? damageable = HitData.damageable;
            IDamageable? baseDamageable = damageable?.GetBaseDamagable();
            if (baseDamageable != null && baseDamageable.GetHealthRel() <= 0) return;

            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            WeaponPatches.ApplyEWCHit(cwc, HitData, out _);
        }
    }
}
