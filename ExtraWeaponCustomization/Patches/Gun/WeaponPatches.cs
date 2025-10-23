using Agents;
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

            CustomWeaponComponent? cwc = ShotManager.ActiveFiringInfo.cgc;
            HitData data = new(weaponRayData, additionalDis);
            if (cwc == null)
            {
                ApplyDebuffs(data);
                return;
            }

            // Can't cache a single object since this function might be called by triggers during a previous call
            ApplyEWCHit(cwc, data, out allowDirectionalBonus);
        }

        private static void ApplyDebuffs(WeaponHitData weaponRayData, float additionalDis) => ApplyDebuffs(new(weaponRayData, additionalDis));
        private static void ApplyDebuffs(HitData data)
        {
            if (data.damageType.HasFlag(DamageType.Dead)) return;

            data.ResetDamage();
            if (DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Damage, data.damageType, DebuffManager.DefaultGroupSet, out var damageMod))
                data.damage *= damageMod.Value;
            if (DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Precision, data.damageType, DebuffManager.DefaultGroupSet, out var precisionMod))
                data.precisionMulti *= precisionMod.Value;
            if (DebuffManager.TryGetShotModDebuff(data.damageable!, StatType.Stagger, data.damageType, DebuffManager.DefaultGroupSet, out var staggerMod))
                data.staggerMulti *= staggerMod.Value;
            data.Apply();
        }
        
        public static void ApplyEWCHit(CustomWeaponComponent cwc, HitData hitData, out bool doBackstab) => ApplyEWCHit(cwc, cwc.GetContextController(), hitData, out doBackstab);

        public static void ApplyEWCHit(CustomWeaponComponent cwc, ContextController cc, HitData hitData, out bool doBackstab)
        {
            hitData.ResetDamage();
            Enemy.EnemyLimbPatches.CacheComponents(cwc, cc);

            doBackstab = cwc.Weapon.AllowBackstab;
            IDamageable? damageable = hitData.damageable;
            if (!hitData.damageType.HasFlag(DamageType.Dead))
            {
                float backstab = 1f;
                float origBackstab = 1f;
                Agent? agent = damageable!.GetBaseAgent();
                Dam_EnemyDamageLimb? limb = null;
                bool enemy = agent != null && agent.Alive && agent.Type == AgentType.Enemy;
                if (enemy)
                {
                    limb = damageable!.Cast<Dam_EnemyDamageLimb>();
                    if (doBackstab)
                    {
                        origBackstab = limb.ApplyDamageFromBehindBonus(1f, hitData.hitPos, hitData.fireDir.normalized);
                        backstab = origBackstab.Map(1f, 2f, 1f, cc.Invoke(new WeaponBackstabContext()).Value);
                    }
                }

                cc.Invoke(new WeaponPreHitDamageableContext(hitData, backstab, origBackstab));

                WeaponStatContext damageContext = new(hitData, cwc.DebuffIDs);
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
                        origBackstab,
                        limb!
                    );

                    cc.Invoke(hitContext);

                    HitTrackerManager.RegisterHit(cwc.Owner, cwc, hitContext);

                    if (backstab > 1f)
                        hitData.damage *= backstab / origBackstab;
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
