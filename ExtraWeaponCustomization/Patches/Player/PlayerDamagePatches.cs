using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using HarmonyLib;
using Player;

namespace EWC.Patches.Player
{
    internal static class PlayerDamagePatches
    {
        private static bool _ignoreCall = false;
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_TakeDamage(Dam_PlayerDamageBase __instance, float damage)
        {
            _ignoreCall = damage <= 0 || __instance.Health <= 0;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_TakeDamage(Dam_PlayerDamageBase __instance, float damage)
        {
            if (_ignoreCall || !PlayerBackpackManager.TryGetBackpack(__instance.Owner.Owner, out var backpack)) return;

            if (backpack.TryGetBackpackItem(InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponDamageTakenContext(damage));
            }

            if (backpack.TryGetBackpackItem(InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(new WeaponDamageTakenContext(damage));
            }
        }
    }
}
