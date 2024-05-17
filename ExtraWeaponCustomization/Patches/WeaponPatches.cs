using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Gear;
using HarmonyLib;
using Player;
using static Weapon;

namespace ExtraWeaponCustomization.Patches
{
    internal static class WeaponPatches
    {
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.SetOwner))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(BulletWeapon __instance)
        {
            if (__instance.Owner == null) return;

            CustomWeaponData? data = CustomWeaponManager.Current.GetCustomWeaponData(__instance.ArchetypeID);
            if (data == null) return;

            if (__instance.gameObject.GetComponent<CustomWeaponComponent>() != null) return;

            CustomWeaponComponent cwc = __instance.gameObject.AddComponent<CustomWeaponComponent>();
            cwc.Register(data);
            cwc.Invoke(new WeaponPostSetupContext(__instance));
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, bool allowDirectionalBonus)
        {
            CustomWeaponComponent? cwc = weaponRayData.owner.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPreHitContext(ref weaponRayData, additionalDis, cwc.Weapon));
            if (doDamage && WeaponPreHitEnemyContext.GetDamageableFromData(weaponRayData)?.GetBaseAgent()?.Type == Agents.AgentType.Enemy)
                cwc.Invoke(new WeaponPreHitEnemyContext(ref weaponRayData, additionalDis, cwc.Weapon));
        }
    }
}
