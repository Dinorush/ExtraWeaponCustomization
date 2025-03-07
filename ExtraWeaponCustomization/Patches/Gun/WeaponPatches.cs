﻿using Agents;
using Enemies;
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
        }

        // Used to correctly apply HitCallback damage modification on piercing shots
        // (otherwise damage mods apply to future pierce shots exponentially)
        private static float s_origHitDamage = 0;
        private static float s_origHitPrecision = 0;
        private readonly static HitData s_hitData = new();
        
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, ref bool allowDirectionalBonus)
        {
            // Sentry filter. Auto has back damage, shotgun does not have vfx, none pass both conditions but guns do
            if (!allowDirectionalBonus || weaponRayData.vfxBulletHit != null || !doDamage) return;

            // All CWC behavior is handled client-side.
            if (!weaponRayData.owner.IsLocallyOwned) return;

            s_hitData.Setup(weaponRayData, additionalDis);
            IDamageable? damageable = s_hitData.damageable;
            IDamageable? damBase = damageable != null ? damageable.GetBaseDamagable() : damageable;
            if (damBase != null && damBase.GetHealthRel() <= 0) return;

            if (damageSearchID != 0 && damBase?.TempSearchID == damageSearchID) return;

            CustomWeaponComponent? cwc = s_hitData.owner.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                if (damageable.IsEnemy())
                    KillTrackerManager.ClearHit(damageable.GetBaseAgent().Cast<EnemyAgent>());
                return;
            }

            // Correct piercing damage back to base damage to apply damage mods
            if (damageable != null)
            {
                if (s_hitData.shotInfo.Hits == 0)
                {
                    s_origHitDamage = s_hitData.damage;
                    s_origHitPrecision = s_hitData.precisionMulti;
                }
                s_hitData.damage = s_origHitDamage;
                s_hitData.precisionMulti = s_origHitPrecision;
            }

            ApplyEWCHit(cwc, s_hitData, ref s_origHitDamage, out allowDirectionalBonus);
        }

        public static void ApplyEWCHit(CustomWeaponComponent cwc, HitData hitData, ref float pierceDamage, out bool doBackstab) => ApplyEWCHit(cwc.GetContextController(), cwc.Weapon, hitData, ref pierceDamage, out doBackstab);

        public static void ApplyEWCHit(ContextController cc, ItemEquippable weapon, HitData hitData, ref float pierceDamage, out bool doBackstab)
        {
            doBackstab = true;
            Enemy.EnemyLimbPatches.CachedCC = cc;

            IDamageable? damageable = hitData.damageable;
            DamageType damageType;
            if (damageable.IsValid() && damageable.GetBaseDamagable().GetHealthRel() > 0)
            {
                float backstab = 1f;
                float origBackstab = 1f;
                Agent? agent = damageable.GetBaseAgent();
                Dam_EnemyDamageLimb? limb = null;
                bool enemy = agent != null && agent.Alive && agent.Type == AgentType.Enemy;
                if (enemy)
                {
                    limb = damageable!.Cast<Dam_EnemyDamageLimb>();
                    origBackstab = limb.ApplyDamageFromBehindBonus(1f, hitData.hitPos, hitData.fireDir.normalized);
                    backstab = origBackstab.Map(1f, 2f, 1f, cc.Invoke(new WeaponBackstabContext()).Value);
                }

                damageType = cc.Invoke(new WeaponPreHitDamageableContext(hitData, backstab, DamageType.Bullet)).DamageType;

                // Modify damage BEFORE pre hit callback so explosion doesn't modify bullet damage
                WeaponDamageContext damageContext = new(hitData.damage, hitData.precisionMulti, damageable);
                cc.Invoke(damageContext);
                hitData.damage = damageContext.Damage.Value;
                hitData.precisionMulti = damageContext.Precision.Value;
                bool bypassCap = Enemy.EnemyLimbPatches.CachedBypassTumorCap = damageContext.BypassTumorCap;

                WeaponPierceContext pierceContext = new(pierceDamage, damageable);
                cc.Invoke(pierceContext);
                pierceDamage = pierceContext.Value;

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

                    KillTrackerManager.RegisterHit(weapon, hitContext);

                    if (backstab > 1f)
                        hitData.damage *= backstab / origBackstab;
                    else
                        doBackstab = false;
                }
                else
                    cc.Invoke(new WeaponHitDamageableContext(hitData, DamageType.Bullet));
            }
            else
                damageType = cc.Invoke(new WeaponHitContext(hitData)).DamageType;

            hitData.shotInfo.AddHit(damageType);
            hitData.Apply();
        }
    }
}
