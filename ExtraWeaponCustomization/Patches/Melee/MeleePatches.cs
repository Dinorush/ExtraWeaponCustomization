using EWC.CustomWeapon;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Patches.Gun;
using EWC.Utils;
using EWC.Utils.Extensions;
using Gear;
using HarmonyLib;
using System;

namespace EWC.Patches.Melee
{
    [HarmonyPatch]
    internal static class MeleePatches
    {
        public readonly static HitData HitData = new(CustomWeapon.Enums.DamageType.Bullet);
        private static CustomWeaponComponent? _cachedSwingCWC = null;
        public static float CachedCharge { get; private set; } = 0f;

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.ChangeState))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_MeleeChangeState(MeleeWeaponFirstPerson __instance, out CustomMeleeComponent? __state)
        {
            if (!__instance.TryGetComp<CustomMeleeComponent>(out __state)) return;

            __state.UpdateAttackSpeed();
        }

        [HarmonyPatch(typeof(MWS_AttackLight), nameof(MWS_AttackLight.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetLightDamage(MWS_AttackLight __instance)
        {
            OnSwingStart(__instance.m_weapon, 0);
        }

        [HarmonyPatch(typeof(MWS_AttackHeavy), nameof(MWS_AttackHeavy.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetHeavyDamage(MWS_AttackHeavy __instance)
        {
            var weapon = __instance.m_weapon;
            var chargeUp = (__instance.m_data.m_side == eMeleeAttackSide.Left ? weapon.m_states[(int)eMeleeWeaponState.AttackChargeUpLeft] : weapon.m_states[(int)eMeleeWeaponState.AttackChargeUpRight]).Cast<MWS_ChargeUp>();
            OnSwingStart(weapon, Math.Min(chargeUp.m_elapsed / chargeUp.m_maxDamageTime, 1f));
        }

        private static void OnSwingStart(MeleeWeaponFirstPerson weapon, float charge)
        {
            _cachedSwingCWC = weapon.GetComponent<CustomWeaponComponent>();
            if (_cachedSwingCWC == null) return;
            ShotManager.AdvanceGroupMod(_cachedSwingCWC);

            CachedCharge = charge;
            if (charge > 0 && charge < 1)
            {
                var context = _cachedSwingCWC.Invoke(new WeaponChargeContext());
                if (context.Exponent != 3)
                    weapon.SetNextDamageToDeal(eMeleeWeaponDamage.Heavy, (float)Math.Pow(charge, context.Exponent));
            }
            HitData.shotInfo.Reset(weapon.m_damageToDeal, weapon.m_precisionMultiToDeal, weapon.m_staggerMultiToDeal, _cachedSwingCWC);
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(MeleeWeaponFirstPerson __instance, MeleeWeaponDamageData data, bool isPush)
        {
            if (isPush || _cachedSwingCWC == null) return;

            HitData.Setup(__instance, data);

            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            WeaponPatches.ApplyEWCHit(cwc, HitData, out _);
        }
    }
}
