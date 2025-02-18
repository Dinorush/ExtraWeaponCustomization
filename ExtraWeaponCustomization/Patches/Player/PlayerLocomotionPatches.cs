using EWC.CustomWeapon;
using HarmonyLib;
using EWC.CustomWeapon.WeaponContext;
using Player;
using EWC.CustomWeapon.WeaponContext.Contexts;

namespace EWC.Patches.Player
{
    [HarmonyPatch]
    internal static class PlayerLocomotionPatches
    {
        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void EnterCrouch(PLOC_Crouch __instance)
        {
            CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponCrouchContext>.Instance);
        }

        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ExitCrouch(PLOC_Crouch __instance)
        {
            CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponCrouchEndContext>.Instance);
        }

        [HarmonyPatch(typeof(PLOC_Run), nameof(PLOC_Run.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void EnterSprint(PLOC_Run __instance)
        {
            CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponSprintContext>.Instance);
        }

        [HarmonyPatch(typeof(PLOC_Run), nameof(PLOC_Run.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ExitSprint(PLOC_Run __instance)
        {
            CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponSprintEndContext>.Instance);
        }

        private static bool _inJump = false;
        [HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void EnterJump(PLOC_Jump __instance)
        {
            _inJump = true;
            CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponJumpContext>.Instance);
        }

        [HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.CommonExit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ExitJump(PLOC_Jump __instance)
        {
            var owner = __instance.m_owner;
            if (owner.IsLocallyOwned && owner.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Fall)
            {
                CustomWeaponManager.InvokeOnGear(owner.Owner, StaticContext<WeaponJumpEndContext>.Instance);
                _inJump = false;
            }
        }

        [HarmonyPatch(typeof(PLOC_Fall), nameof(PLOC_Fall.Enter))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void EnterFall(PLOC_Fall __instance)
        {
            if (!_inJump)
            {
                CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponJumpContext>.Instance);
                _inJump = true;
            }
        }

        [HarmonyPatch(typeof(PLOC_Fall), nameof(PLOC_Fall.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void ExitFall(PLOC_Fall __instance)
        {
            CustomWeaponManager.InvokeOnGear(__instance.m_owner.Owner, StaticContext<WeaponJumpEndContext>.Instance);
            _inJump = false;
        }
    }
}
