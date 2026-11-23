using EWC.CustomWeapon;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.HitTracker;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using EWC.Utils.Extensions;
using Gear;
using HarmonyLib;
using static Weapon;

namespace EWC.Patches.Gun
{
    [HarmonyPatch]
    internal static class WeaponPatches
    {
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnGearSpawn(BulletWeapon __instance)
        {
            CustomWeaponManager.AddSpawnedItem(__instance);
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.ReloadTime), MethodType.Getter)]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostGetReloadTime(BulletWeapon __instance, ref float __result)
        {
            if (!__instance.TryGetComp<CustomGunComponent>(out var cgc)) return;

            __result /= cgc.Invoke(new WeaponReloadContext()).Value;
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.SetCurrentClip))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void UpdateClip(BulletWeapon __instance, int clip, ref bool __state)
        {
            if (__instance == null) return; // Apparently this can happen?

            __state = __instance.m_clip != clip;
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.SetCurrentClip))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void UpdateClip(BulletWeapon __instance, bool __state)
        {
            if (__instance == null || !__state) return;

            if (!__instance.TryGetComp<CustomGunComponent>(out var cwc)) return;

            cwc.Invoke(new WeaponAmmoContext(__instance.m_clip, __instance.ClipSize));
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, ref bool allowDirectionalBonus)
        {
            if (!doDamage) return;

            IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(weaponRayData.rayHit);
            IDamageable? damBase = damageable != null ? damageable.GetBaseDamagable() : damageable;
            if (damageSearchID != 0 && damBase?.TempSearchID == damageSearchID) return;

            CustomWeaponComponent? cwc = ShotManager.ActiveFiringInfo.cwc;
            HitData data = new(weaponRayData, additionalDis, ShotManager.ActiveFiringInfo.archBlock?.PierceLimit() ?? 1);
            if (cwc == null)
            {
                ApplyDebuffs(data, allowDirectionalBonus);
                return;
            }

            // Can't cache a single object since this function might be called by triggers during a previous call
            ApplyEWCHit(cwc, data, out allowDirectionalBonus);
        }

        private static void ApplyDebuffs(HitData data, bool allowBackstab)
        {
            if (data.damageType.HasFlag(DamageType.Dead)) return;

            data.ResetDamage();
            if (DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Damage, data.damageType, DebuffManager.DefaultGroupSet, out var damageMod))
                data.damage *= damageMod.Value;
            if (DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Precision, data.damageType, DebuffManager.DefaultGroupSet, out var precisionMod))
                data.precisionMulti *= precisionMod.Value;
            if (DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Stagger, data.damageType, DebuffManager.DefaultGroupSet, out var staggerMod))
                data.staggerMulti *= staggerMod.Value;
            if (allowBackstab && data.damageType.HasFlag(DamageType.Enemy) && DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Backstab, data.damageType, DebuffManager.DefaultGroupSet, out var backstabMod))
            {
                var origBackstab = data.damageable!.Cast<Dam_EnemyDamageLimb>().ApplyDamageFromBehindBonus(1f, data.hitPos, data.fireDir.normalized);
                data.damage *= DamageableUtil.MergeBackstabMulti(origBackstab, origBackstab);
            }
            data.Apply();
        }
        
        public static void ApplyEWCHit(CustomWeaponComponent cwc, HitData hitData, out bool doBackstab) => ApplyEWCHit(cwc, cwc.GetContextController(), hitData, out doBackstab);

        public static void ApplyEWCHit(CustomWeaponComponent cwc, ContextController cc, HitData hitData, out bool doBackstab)
        {
            hitData.ResetDamage();
            Enemy.EnemyLimbPatches.CacheComponents(cwc, cc);

            doBackstab = cwc.Weapon.AllowBackstab;
            IDamageable? damageable = hitData.damageable;
            var damageType = hitData.damageType;
            if (!damageType.HasFlag(DamageType.Dead))
            {
                float origBackstab = 1f;
                Dam_EnemyDamageLimb? limb = null;
                if (damageType.HasFlag(DamageType.Enemy))
                {
                    limb = damageable!.Cast<Dam_EnemyDamageLimb>();
                    if (doBackstab)
                        origBackstab = limb.ApplyDamageFromBehindBonus(1f, hitData.hitPos, hitData.fireDir.normalized);
                }

                cc.Invoke(new WeaponPreHitDamageableContext(hitData, origBackstab));

                WeaponStatContext statContext = new(hitData, cwc.DebuffIDs);
                cc.Invoke(statContext);
                hitData.damage = statContext.Damage;
                if (damageType.HasFlag(DamageType.Player))
                    hitData.damage *= cwc.Weapon.FriendlyFireMulti;
                hitData.staggerMulti = statContext.Stagger;
                hitData.precisionMulti = statContext.Precision;
                float backstabMulti = DamageableUtil.MergeBackstabMulti(origBackstab, statContext.Backstab);
                bool bypassCap = Enemy.EnemyLimbPatches.CachedBypassTumorCap = statContext.BypassTumorCap;

                if (damageType.HasFlag(DamageType.Enemy))
                {
                    WeaponHitDamageableContext hitContext = new(
                        hitData,
                        bypassCap,
                        origBackstab,
                        limb!
                    );

                    cc.Invoke(hitContext);

                    HitTrackerManager.RegisterHit(cwc.Owner, cwc, hitContext);

                    if (backstabMulti > 1f)
                        hitData.damage *= backstabMulti / origBackstab;
                    else
                        doBackstab = false;
                }
                else
                    cc.Invoke(new WeaponHitDamageableContext(hitData));
            }
            else
                cc.Invoke(new WeaponHitContext(hitData));

            hitData.shotInfo.AddHit(hitData.damageType);
            hitData.Apply();
        }
    }
}
