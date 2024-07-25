using Agents;
using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.KillTracker;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Gear;
using HarmonyLib;
using SNetwork;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.Patches
{
    [HarmonyPatch]
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
            _lastSearchID = 0;
        }

        // Used to correctly apply HitCallback damage modification on piercing shots
        // (otherwise damage mods apply to future pierce shots exponentially)
        private static uint _lastSearchID = 0;
        private static float _origHitDamage = 0;
        public static CustomWeaponComponent? CachedHitCWC { get; private set; }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, bool allowDirectionalBonus)
        {
            CachedHitCWC = null;
            // Sentry filter. Auto has back damage, shotgun does not have vfx, none pass both conditions but guns do
            if (!allowDirectionalBonus || weaponRayData.vfxBulletHit != null || !doDamage) return;

            // Bot/other player filter. All CWC behavior is handled client-side.
            if (!weaponRayData.owner.IsLocallyOwned && (!SNet.IsMaster || !weaponRayData.owner.Owner.IsBot)) return;

            CustomWeaponComponent? cwc = weaponRayData.owner.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            IDamageable? damageable = DamageableUtil.GetDamageableFromData(weaponRayData);
            IDamageable? damBase = damageable?.GetBaseDamagable() != null ? damageable.GetBaseDamagable() : damageable;
            if (damageSearchID != 0 && damBase?.TempSearchID == damageSearchID) return;

            // Correct piercing damage back to base damage to apply damage mods
            if (damageable != null && damageSearchID != 0)
            {
                if (_lastSearchID != damageSearchID)
                {
                    _lastSearchID = damageSearchID;
                    _origHitDamage = weaponRayData.damage;
                }
                weaponRayData.damage = _origHitDamage;
            }

            ApplyEWCBulletHit(cwc, damageable, ref weaponRayData, additionalDis, damageSearchID, ref _origHitDamage);
        }

        public static void ApplyEWCBulletHit(CustomWeaponComponent cwc, IDamageable? damageable, ref WeaponHitData weaponRayData, float additionalDis, uint damageSearchID, ref float pierceDamage)
        {
            CachedHitCWC = cwc;
            
            if (damageable != null)
            {
                // Modify damage BEFORE pre hit callback so explosion doesn't modify bullet damage
                WeaponDamageContext damageContext = new(weaponRayData.damage, weaponRayData.precisionMulti, damageable, cwc.Weapon);
                cwc.Invoke(damageContext);
                weaponRayData.damage = damageContext.Damage.Value;
                weaponRayData.precisionMulti = damageContext.Precision.Value;

                if (damageSearchID != 0)
                {
                    WeaponPierceContext pierceContext = new(pierceDamage, damageable, cwc.Weapon);
                    cwc.Invoke(pierceContext);
                    pierceDamage = pierceContext.Value;
                }
            }

            Agent? agent = damageable?.GetBaseAgent();
            if (agent != null && agent.Type == AgentType.Enemy && agent.Alive)
            {
                Dam_EnemyDamageLimb? limb = damageable!.TryCast<Dam_EnemyDamageLimb>()!;
                WeaponPreHitEnemyContext hitContext = new(
                    weaponRayData,
                    additionalDis,
                    limb,
                    cwc.Weapon,
                    DamageType.Bullet
                    );

                cwc.Invoke(hitContext);

                Vector3 localPos = weaponRayData.rayHit.point - damageable.GetBaseAgent().Position;
                KillTrackerManager.RegisterHit(agent, localPos, cwc.Weapon, hitContext.DamageType);
            }
            else
                cwc.Invoke(new WeaponPreHitContext(weaponRayData, additionalDis, cwc.Weapon));
        }
    }
}
