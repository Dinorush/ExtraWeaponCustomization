using Agents;
using EWC.CustomWeapon;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using EWC.Utils.Extensions;
using Gear;
using HarmonyLib;
using static Weapon;

namespace EWC.Patches
{
    [HarmonyPatch]
    internal static class WeaponPatches
    {
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SetupCallback(BulletWeapon __instance)
        {
            if (__instance.ArchetypeData == null) return;

            CustomWeaponManager.Current.AddWeaponListener(__instance);
            if (!CustomWeaponManager.TryGetCustomGunData(__instance.ArchetypeData.persistentID, out var data)) return;

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

            if (cwc.SpreadController != null)
                cwc.SpreadController.Active = true;
            cwc.Invoke(StaticContext<WeaponWieldContext>.Instance);
            cwc.RefreshSoundDelay();
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.OnUnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateWeaponUnwielded(BulletWeapon __instance)
        {
            CustomWeaponComponent? cwc = __instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(StaticContext<WeaponUnWieldContext>.Instance);
            if (cwc.SpreadController != null)
                cwc.SpreadController.Active = false;
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, ref bool allowDirectionalBonus)
        {
            // Sentry filter. Auto has back damage, shotgun does not have vfx, none pass both conditions but guns do
            if (!doDamage || !allowDirectionalBonus || weaponRayData.vfxBulletHit != null) return;

            // All CWC behavior is handled client-side.
            if (!weaponRayData.owner.IsLocallyOwned) return;

            IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(weaponRayData.rayHit);
            IDamageable? damBase = damageable != null ? damageable.GetBaseDamagable() : damageable;
            if (damBase != null && damBase.GetHealthRel() <= 0) return;

            if (damageSearchID != 0 && damBase?.TempSearchID == damageSearchID) return;

            CustomWeaponComponent? cwc = weaponRayData.owner.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            // Can't cache a single object since this function might be called by triggers during a previous call
            HitData data = new(weaponRayData, cwc, additionalDis);
            ApplyEWCHit(cwc, data, out allowDirectionalBonus);
        }

        public static void ApplyEWCHit(CustomWeaponComponent cwc, HitData hitData, out bool doBackstab) => ApplyEWCHit(cwc, cwc.GetContextController(), hitData, out doBackstab);

        public static void ApplyEWCHit(CustomWeaponComponent cwc, ContextController cc, HitData hitData, out bool doBackstab)
        {
            hitData.ResetDamage();
            doBackstab = true;
            Enemy.EnemyLimbPatches.CachedCC = cc;

            IDamageable? damageable = hitData.damageable;
            bool isValid = damageable.IsValid();
            bool alive = isValid && damageable!.GetBaseDamagable().GetHealthRel() > 0;
            if (alive)
            {
                float backstab = 1f;
                float origBackstab = 1f;
                Agent? agent = damageable!.GetBaseAgent();
                Dam_EnemyDamageLimb? limb = null;
                bool enemy = agent != null && agent.Alive && agent.Type == AgentType.Enemy;
                if (enemy)
                {
                    limb = damageable!.Cast<Dam_EnemyDamageLimb>();
                    origBackstab = limb.ApplyDamageFromBehindBonus(1f, hitData.hitPos, hitData.fireDir.normalized);
                    backstab = origBackstab.Map(1f, 2f, 1f, cc.Invoke(new WeaponBackstabContext()).Value);
                }

                cc.Invoke(new WeaponPreHitDamageableContext(hitData, backstab));

                // Modify damage BEFORE pre hit callback so explosion doesn't modify bullet damage
                WeaponStatContext damageContext = new(hitData);
                cc.Invoke(damageContext);
                hitData.damage = damageContext.Damage;
                hitData.staggerMulti = damageContext.Stagger;
                hitData.precisionMulti = damageContext.Precision;
                bool bypassCap = Enemy.EnemyLimbPatches.CachedBypassTumorCap = damageContext.BypassTumorCap;

                if (enemy)
                {
                    WeaponHitDamageableContext hitContext = new(
                        hitData,
                        bypassCap,
                        backstab,
                        limb!,
                        DamageType.Bullet
                    );

                    cc.Invoke(hitContext);

                    KillTrackerManager.RegisterHit(cwc, hitContext);

                    if (backstab > 1f)
                        hitData.damage *= backstab / origBackstab;
                    else
                        doBackstab = false;
                }
                else
                    cc.Invoke(new WeaponHitDamageableContext(hitData));
            }
            else
                cc.Invoke(new WeaponHitContext(hitData, isValid && !alive));

            hitData.shotInfo.AddHit(hitData.damageType);
            hitData.Apply();
        }
    }
}
