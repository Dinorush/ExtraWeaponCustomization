using EWC.CustomWeapon;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils.Extensions;
using GameData;
using Gear;
using HarmonyLib;
using MSC.CustomMeleeData;

namespace EWC.Patches.Melee
{
    [HarmonyPatch]
    internal static class MWSPatches
    {
        private static float _cacheChargeDiff = -1;
        private static MSCWrapper.MSCData _cacheData = new();

        [HarmonyPatch(typeof(MWS_ChargeUp), nameof(MWS_ChargeUp.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ChargeCallback(MWS_ChargeUp __instance)
        {
            if (!__instance.m_weapon.TryGetComp<CustomMeleeComponent>(out var cmc)) return;

            _cacheChargeDiff = __instance.m_maxDamageTime;
            __instance.m_maxDamageTime /= cmc.CurrentAttackSpeed;
            _cacheChargeDiff -= __instance.m_maxDamageTime;
            var animData = __instance.m_weapon.MeleeAnimationData;
            animData.AutoAttackTime -= _cacheChargeDiff;
            animData.AutoAttackWarningTime -= _cacheChargeDiff;
        }

        [HarmonyPatch(typeof(MWS_ChargeUp), nameof(MWS_ChargeUp.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void RestoreAutoAttackTimings(MWS_ChargeUp __instance)
        {
            if (_cacheChargeDiff == -1) return;

            var animData = __instance.m_weapon.MeleeAnimationData;
            animData.AutoAttackTime += _cacheChargeDiff;
            animData.AutoAttackWarningTime += _cacheChargeDiff;
            _cacheChargeDiff = -1;
        }

        [HarmonyPatch(typeof(MWS_AttackSwingBase), nameof(MWS_AttackSwingBase.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PreSwingCallback(MWS_AttackSwingBase __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponPreFireContext>.Instance);
        }

        [HarmonyPatch(typeof(MWS_AttackSwingBase), nameof(MWS_AttackSwingBase.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostSwingCallback(MWS_AttackSwingBase __instance)
        {
            CustomWeaponComponent? cwc = __instance.m_weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponShotEndContext(CustomWeapon.Enums.DamageType.Bullet, MeleePatches.HitData.shotInfo, null));
            cwc.Invoke(StaticContext<WeaponPostFireContext>.Instance);
        }
    }
}
