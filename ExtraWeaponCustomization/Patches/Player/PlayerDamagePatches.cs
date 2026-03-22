using EWC.CustomWeapon;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.WeaponContext.Contexts;
using HarmonyLib;

namespace EWC.Patches.Player
{
    [HarmonyPatch]
    internal static class PlayerDamagePatches
    {
        private static PlayerDamageType _damageType = PlayerDamageType.Any;
        private static float _currentDamage = -1f;
        private static bool _currentImmune = false;

        public static void SetPlayerDamageType(PlayerDamageType damageType) => _damageType = damageType;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveTentacleAttackDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveTentacleAttackDamage))]
        [HarmonyPrefix]
        private static void Pre_TentacleDamage() => _damageType = PlayerDamageType.Tentacle;

        // NOT inlined
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveMeleeDamage))]
        [HarmonyPrefix]
        private static void Pre_MeleeDamage() => _damageType = PlayerDamageType.Melee;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveShooterProjectileDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveShooterProjectileDamage))]
        [HarmonyPrefix]
        private static void Pre_ShooterDamage() => _damageType = PlayerDamageType.Shooter;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveExplosionDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveExplosionDamage))]
        [HarmonyPrefix]
        private static void Pre_ExplosionDamage() => _damageType = PlayerDamageType.Explosive | PlayerDamageType.Enemy;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveFireDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveFireDamage))]
        [HarmonyPrefix]
        private static void Pre_BleedDamage() => _damageType = PlayerDamageType.Bleed;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveBulletDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveBulletDamage))]
        [HarmonyPrefix]
        private static void Pre_BulletDamage() => _damageType = PlayerDamageType.Bullet;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveNoAirDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveNoAirDamage))]
        [HarmonyPrefix]
        private static void Pre_SyringeDamage() => _damageType = PlayerDamageType.Syringe;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveFallDamage))]
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveFallDamage))]
        [HarmonyPrefix]
        private static void Pre_FallDamage() => _damageType = PlayerDamageType.Fall;

        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_TakeDamage(Dam_PlayerDamageBase __instance, ref float damage, ref bool __state)
        {
            if (DebuffManager.TryGetArmorModBuff(__instance.Cast<IDamageable>(), _damageType, out _currentImmune, out var mod))
            {
                if (_currentImmune)
                    damage = 0;
                else
                    damage = mod > 0 ? damage / mod : float.PositiveInfinity;
            }

            _currentDamage = damage;
            var owner = __instance.Owner.Owner;
            __state = damage > 0 && __instance.Health > 0 && (owner.IsLocal || (owner.IsBot && SNetwork.SNet.IsMaster));
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_TakeDamage(Dam_PlayerDamageBase __instance, float damage, bool __state)
        {
            var owner = __instance.Owner.Owner;
            if (__state)
                CustomWeaponManager.InvokeOnGear(owner, new WeaponDamageTakenContext(damage, _damageType));
            if (!owner.IsLocal)
                ResetAttackData();
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.Hitreact))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_Hitreact(ref float damage)
        {
            if (_currentDamage < 0) return true;

            damage = _currentDamage;
            var allow = !_currentImmune;
            ResetAttackData();
            return allow;
        }

        private static void ResetAttackData()
        {
            _damageType = PlayerDamageType.Any;
            _currentDamage = -1;
            _currentImmune = false;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
        [HarmonyPrefix]
        private static void Pre_ReceiveHealth(Dam_PlayerDamageBase __instance, ref float __state)
        {
            __state = __instance.Health;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_ReceiveHealth(Dam_PlayerDamageBase __instance, float __state)
        {
            if (__state == __instance.Health) return;

            CustomWeaponManager.InvokeOnGear(__instance.Owner.Owner, new WeaponHealthContext(__instance));
        }
    }
}
