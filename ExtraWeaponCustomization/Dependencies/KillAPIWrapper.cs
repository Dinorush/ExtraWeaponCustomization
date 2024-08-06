using Enemies;
using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.KillTracker;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using KillIndicatorFix;
using UnityEngine;

namespace ExtraWeaponCustomization.Dependencies
{
    public static class KillAPIWrapper
    {
        public const string PLUGIN_GUID = "randomuserhi.KillIndicatorFix";

        public static void Init()
        {
            Kill.OnKillIndicator += RunKillCallbacks;
        }

        public static void TagEnemy(EnemyAgent enemy, ItemEquippable? item = null, Vector3? localHitPosition = null) => Kill.TagEnemy(enemy, item, localHitPosition);

        private const long MAX_DELAY = 500;
        public static void RunKillCallbacks(EnemyAgent enemy, ItemEquippable? item, long delay)
        {
            if (delay > MAX_DELAY) return;

            WeaponPreHitEnemyContext? hitContext = KillTrackerManager.GetKillHitContext(enemy);
            if (hitContext == null) return;

            BulletWeapon weapon = hitContext.Weapon;
            CustomWeaponComponent? cwc = weapon.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPostKillContext(hitContext));
        }
    }
}
