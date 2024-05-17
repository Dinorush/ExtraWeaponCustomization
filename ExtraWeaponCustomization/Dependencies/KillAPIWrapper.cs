using BepInEx.Unity.IL2CPP;
using Enemies;
using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using KillIndicatorFix;

namespace ExtraWeaponCustomization.Dependencies
{
    public static class KillAPIWrapper
    {
        public const string PLUGIN_GUID = "randomuserhi.KillIndicatorFix";
        public static bool HasKillAPI
        {
            get
            {
                return IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
            }
        }

        public static void Init()
        {
            // Hard dependency for now, so no need to check
            //if (HasKillAPI)
            UnsafeInit();
        }

        private static void UnsafeInit()
        {
            Kill.OnKillIndicator += RunKillCallbacks;
        }

        private const long MAX_DELAY = 500;
        public static void RunKillCallbacks(EnemyAgent enemy, ItemEquippable? item, long delay)
        {
            if (delay > MAX_DELAY) return;

            CustomWeaponComponent? cwc = item?.TryCast<BulletWeapon>()?.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return;

            cwc.Invoke(new WeaponPostKillContext(enemy, cwc.Weapon));
        }
    }
}
