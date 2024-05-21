using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.KillTracker;
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
        private static void SetupCallback(BulletWeapon __instance, PlayerAgent agent)
        {
            if (agent == null) return;

            CustomWeaponData? data = CustomWeaponManager.Current.GetCustomWeaponData(__instance.ArchetypeID);
            if (data == null) return;

            if (__instance.gameObject.GetComponent<CustomWeaponComponent>() != null) return;

            CustomWeaponComponent cwc = __instance.gameObject.AddComponent<CustomWeaponComponent>();
            cwc.Register(data);
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, bool allowDirectionalBonus)
        {
            // Sentry filter. Auto has back damage, shotgun does not have vfx, none pass both conditions but guns do
            if (!allowDirectionalBonus || weaponRayData.vfxBulletHit != null) return;

            CustomWeaponComponent? cwc = weaponRayData.owner.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            IDamageable? damageable = WeaponTriggerContext.GetDamageableFromData(weaponRayData);
            IDamageable? damBase = damageable?.GetBaseDamagable() != null ? damageable.GetBaseDamagable() : damageable;
            if (damageSearchID != 0 && damBase != null && damBase.TempSearchID == damageSearchID) return;

            cwc.Invoke(new WeaponPreHitContext(ref weaponRayData, additionalDis, cwc.Weapon));
            if (doDamage && damageable?.GetBaseAgent()?.Type == Agents.AgentType.Enemy)
            {
                WeaponDamageContext damageContext = new(weaponRayData.damage, damageable, cwc.Weapon);
                cwc.Invoke(damageContext);
                weaponRayData.damage = damageContext.Damage;

                Dam_EnemyDamageLimb? limb = damageable.TryCast<Dam_EnemyDamageLimb>();
                bool precHit = limb != null && limb.m_type == eLimbDamageType.Weakspot;
                cwc.Invoke(new WeaponPreHitEnemyContext(
                    weaponRayData.Falloff(additionalDis),
                    damageable,
                    cwc.Weapon,
                    precHit ? TriggerType.OnPrecHitBullet : TriggerType.OnHitBullet
                    ));

                KillTrackerManager.RegisterHit(damageable.GetBaseAgent(), cwc.Weapon, precHit);
            }
        }
    }
}
