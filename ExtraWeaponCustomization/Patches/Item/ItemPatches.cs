using EWC.Attributes;
using EWC.CustomWeapon;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using Gear;
using HarmonyLib;
using Player;

namespace EWC.Patches.Item
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        [InvokeOnLoad]
        private static void Init() => CustomWeaponManager.OnResetCWCs += ReloadLocalCWC;
        private static void ReloadLocalCWC(bool activate)
        {
            _localCWC = null;
            if (!activate || !PlayerManager.HasLocalPlayerAgent()) return;

            var wielded = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedItem;
            if (wielded != null && wielded.TryGetComp<CustomWeaponComponent>(out var cwc))
                _localCWC = cwc;
        }

        [InvokeOnCleanup]
        private static void OnCleanup() => _localCWC = null;

        private static CustomWeaponComponent? _localCWC;

        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.OnDestroy))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnGearDestroyed(ItemEquippable __instance)
        {
            CustomWeaponManager.RemoveSpawnedItem(__instance);
        }

        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.OnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateCurrentWeapon(ItemEquippable __instance)
        {
            if (!__instance.TryGetComp<CustomWeaponComponent>(out var cwc)) return;

            cwc.OnWield();
            if (cwc.Owner.IsType(OwnerType.Local))
                _localCWC = cwc;
        }

        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.OnUnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateWeaponUnwielded(BulletWeapon __instance)
        {
            if (!__instance.TryGetComp<CustomWeaponComponent>(out var cwc)) return;

            cwc.OnUnWield();
            if (cwc.Owner.IsType(OwnerType.Local))
                _localCWC = null;
        }

        private static bool _inRunCheck = false;
        [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.ItemCanMoveQuick))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool PreGetRunAllowed(ref bool __result)
        {
            _inRunCheck = true;
            if (_localCWC == null) return true;

            var results = _localCWC.Invoke(new WeaponPreSprintContext());
            if (!results.Allow)
            {
                __result = false;
                return false;
            }
            // Directly modify fire mode and restore using cached FireMode on gun component.
            if (results.AllowBurstCancel)
                _localCWC.Weapon.ArchetypeData.FireMode = eWeaponFireMode.Semi;
            return true;
        }

        [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.ItemCanMoveQuick))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostGetRunAllowed()
        {
            _inRunCheck = false;
            if (_localCWC == null || !_localCWC.Weapon.IsType(WeaponType.Gun)) return;

            // FireMode is cached on Gun component and no callbacks run between the prefix/postfix.
            var gun = (LocalGunComp)_localCWC.Weapon;
            gun.ArchetypeData.FireMode = gun.FireMode;
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.RunAllowed), MethodType.Getter)]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.RunAllowed), MethodType.Getter)]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostGetRunAllowed(ref bool __result)
        {
            if (_inRunCheck || _localCWC == null) return;

            __result = _localCWC.Invoke(new WeaponPreSwapContext(__result)).Allow;
        }
    }
}
