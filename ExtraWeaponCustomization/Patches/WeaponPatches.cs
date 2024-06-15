using Agents;
using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.KillTracker;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Gear;
using HarmonyLib;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.Patches
{
    internal static class WeaponPatches
    {
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(BulletWeapon __instance)
        {
            CustomWeaponManager.Current.AddWeaponListener(__instance);
            CustomWeaponData? data = CustomWeaponManager.Current.GetCustomWeaponData(__instance.ArchetypeData.persistentID);
            if (data == null) return;

            if (__instance.gameObject.GetComponent<CustomWeaponComponent>() != null) return;

            CustomWeaponComponent cwc = __instance.gameObject.AddComponent<CustomWeaponComponent>();
            cwc.Register(data);
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.OnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateCurrentWeapon(BulletWeapon __instance)
        {
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponWieldContext(__instance));
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
            if (damageSearchID != 0 && damBase?.TempSearchID == damageSearchID) return;

            if (doDamage && damageable != null)
            {
                // Modify damage BEFORE pre hit callback so explosion doesn't modify bullet damage
                WeaponDamageContext damageContext = new(weaponRayData.damage, damageable, cwc.Weapon);
                cwc.Invoke(damageContext);
                weaponRayData.damage = damageContext.Value;
            }

            cwc.Invoke(new WeaponPreHitContext(ref weaponRayData, additionalDis, cwc.Weapon));

            Agent? agent = damageable?.GetBaseAgent();
            if (doDamage && agent != null && agent.Type == AgentType.Enemy && agent.Alive)
            {
                Dam_EnemyDamageLimb? limb = damageable!.TryCast<Dam_EnemyDamageLimb>();
                bool precHit = limb != null && limb.m_type == eLimbDamageType.Weakspot;
                cwc.Invoke(new WeaponPreHitEnemyContext(
                    weaponRayData.Falloff(additionalDis),
                    limb != null ? limb.ApplyDamageFromBehindBonus(1f, weaponRayData.rayHit.point, weaponRayData.fireDir.normalized) : 1f,
                    damageable,
                    cwc.Weapon,
                    precHit ? TriggerType.OnPrecHitBullet : TriggerType.OnHitBullet
                    ));

                if (limb != null)
                {
                    cwc.Invoke(new WeaponOnDamageContext(
                        weaponRayData,
                        additionalDis,
                        limb,
                        cwc.Weapon,
                        precHit ? TriggerType.OnPrecDamage : TriggerType.OnDamage
                        ));
                }
                
                Vector3 localPos = weaponRayData.rayHit.point - damageable.GetBaseAgent().Position;
                KillTrackerManager.RegisterHit(agent, localPos, cwc.Weapon, precHit);
            }
        }
    }
}
