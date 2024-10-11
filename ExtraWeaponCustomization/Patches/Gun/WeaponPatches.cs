using Agents;
using Enemies;
using EWC.CustomWeapon;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using Gear;
using HarmonyLib;
using SNetwork;
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
            CustomWeaponManager.Current.AddWeaponListener(__instance);
            CustomWeaponData? data = CustomWeaponManager.Current.GetCustomGunData(__instance.ArchetypeData.persistentID);
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

            cwc.Invoke(StaticContext<WeaponWieldContext>.Instance);
            cwc.RefreshSoundDelay();
            s_lastSearchID = 0;
        }

        // Used to correctly apply HitCallback damage modification on piercing shots
        // (otherwise damage mods apply to future pierce shots exponentially)
        private static uint s_lastSearchID = 0;
        private static float s_origHitDamage = 0;
        private static float s_origHitPrecision = 0;
        private readonly static HitData s_hitData = new();

        private static CustomWeaponComponent? _cachedHitCWC = null;
        public static CustomWeaponComponent? CachedHitCWC
        {
            get { return _cachedHitCWC; }
            set { _cachedHitCWC = value; CachedBypassTumorCap = false; }
        }

        public static bool CachedBypassTumorCap { get; private set; } = false;

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void HitCallback(ref WeaponHitData weaponRayData, bool doDamage, float additionalDis, uint damageSearchID, ref bool allowDirectionalBonus)
        {
            CachedHitCWC = null;
            // Sentry filter. Auto has back damage, shotgun does not have vfx, none pass both conditions but guns do
            if (!allowDirectionalBonus || weaponRayData.vfxBulletHit != null || !doDamage) return;

            // Bot/other player filter. All CWC behavior is handled client-side.
            if (!weaponRayData.owner.IsLocallyOwned && (!SNet.IsMaster || !weaponRayData.owner.Owner.IsBot)) return;

            s_hitData.Setup(weaponRayData, additionalDis);
            IDamageable? damageable = s_hitData.damageable;
            IDamageable? damBase = damageable?.GetBaseDamagable() != null ? damageable.GetBaseDamagable() : damageable;
            if (damageSearchID != 0 && damBase?.TempSearchID == damageSearchID) return;

            CustomWeaponComponent? cwc = s_hitData.owner.Inventory.WieldedItem?.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                Agent? agent = damageable?.GetBaseAgent();
                if (agent != null && agent.Type == AgentType.Enemy && agent.Alive)
                    KillTrackerManager.ClearHit(agent.TryCast<EnemyAgent>()!);
                return;
            }

            // Correct piercing damage back to base damage to apply damage mods
            if (damageable != null && damageSearchID != 0)
            {
                if (s_lastSearchID != damageSearchID)
                {
                    s_lastSearchID = damageSearchID;
                    s_origHitDamage = s_hitData.damage;
                    s_origHitPrecision = s_hitData.precisionMulti;
                }
                s_hitData.damage = s_origHitDamage;
                s_hitData.precisionMulti = s_origHitPrecision;
            }

            ApplyEWCHit(cwc, damageable, s_hitData, damageSearchID != 0, ref s_origHitDamage, ref allowDirectionalBonus);
        }

        public static void ApplyEWCHit(CustomWeaponComponent cwc, IDamageable? damageable, HitData hitData, bool pierce, ref float pierceDamage, ref bool doBackstab)
        {
            CachedHitCWC = cwc;

            if (damageable != null)
            {
                // Modify damage BEFORE pre hit callback so explosion doesn't modify bullet damage
                WeaponDamageContext damageContext = new(hitData.damage, hitData.precisionMulti, damageable);
                cwc.Invoke(damageContext);
                hitData.damage = damageContext.Damage.Value;
                hitData.precisionMulti = damageContext.Precision.Value;
                CachedBypassTumorCap = damageContext.BypassTumorCap;

                if (pierce)
                {
                    WeaponPierceContext pierceContext = new(pierceDamage, damageable);
                    cwc.Invoke(pierceContext);
                    pierceDamage = pierceContext.Value;
                }
            }

            Agent? agent = damageable?.GetBaseAgent();
            if (agent != null && agent.Type == AgentType.Enemy && agent.Alive)
            {
                Dam_EnemyDamageLimb? limb = damageable!.TryCast<Dam_EnemyDamageLimb>()!;

                float backstab = limb.ApplyDamageFromBehindBonus(1f, hitData.hitPos, hitData.fireDir.normalized);
                WeaponBackstabContext backContext = new();
                cwc.Invoke(backContext);

                WeaponPreHitEnemyContext hitContext = new(
                    hitData,
                    CachedBypassTumorCap,
                    backstab.Map(1f, 2f, 1f, backContext.Value),
                    limb,
                    DamageType.Bullet
                );
                cwc.Invoke(hitContext);
                KillTrackerManager.RegisterHit(cwc.Weapon, hitContext);

                if (backContext.Value > 1f)
                    hitData.damage *= hitContext.Backstab / backstab;
                else
                    doBackstab = false;
            }
            else
                cwc.Invoke(new WeaponPreHitContext(hitData));

            hitData.Apply();
        }
    }
}
