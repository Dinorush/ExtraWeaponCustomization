using EWC.CustomWeapon;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext;
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

        [HarmonyPatch(typeof(MWS_Push), nameof(MWS_Push.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PrePushCallback(MWS_Push __instance)
        {
            var weapon = __instance.m_weapon;
            _cachedSwingCWC = weapon.GetComponent<CustomWeaponComponent>(); ;
            if (_cachedSwingCWC == null) return;

            _cachedSwingCWC.Invoke(StaticContext<WeaponPrePushContext>.Instance);
            HitData.shotInfo.SetToPush();
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
            _cachedSwingCWC.Invoke(StaticContext<WeaponPreFireContext>.Instance);
        }

        [HarmonyPatch(typeof(MWS_Push), nameof(MWS_Push.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostPushCallback(MWS_Push __instance)
        {
            if (_cachedSwingCWC == null) return;

            _cachedSwingCWC.Invoke(StaticContext<WeaponPostPushContext>.Instance);
        }

        [HarmonyPatch(typeof(MWS_AttackSwingBase), nameof(MWS_AttackSwingBase.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostSwingCallback(MWS_AttackSwingBase __instance)
        {
            if (_cachedSwingCWC == null) return;

            _cachedSwingCWC.Invoke(new WeaponShotEndContext(DamageType.Bullet, HitData.shotInfo, null));
            _cachedSwingCWC.Invoke(StaticContext<WeaponPostFireContext>.Instance);
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(MeleeWeaponFirstPerson __instance, MeleeWeaponDamageData data, bool isPush)
        {
            if (_cachedSwingCWC == null) return;

            HitData.Setup(__instance, data);

            if (isPush)
            {
                if (HitData.damageType.HasFlag(DamageType.Dead) || !HitData.damageType.HasFlag(DamageType.Enemy)) return;

                var damBase = HitData.damageable!.GetBaseDamagable().Cast<Dam_EnemyDamageBase>();
                if (damBase.IsImortal || !damBase.Owner.EnemyBalancingData.CanBePushed) return;

                HitData.ResetDamage();
                _cachedSwingCWC.Invoke(new WeaponPushHitContext(HitData));
            }
            else
                WeaponPatches.ApplyEWCHit(_cachedSwingCWC, HitData, out _);
        }
    }
}
