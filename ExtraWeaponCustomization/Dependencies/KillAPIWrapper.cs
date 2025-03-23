using BepInEx.Unity.IL2CPP;
using Enemies;
using EWC.CustomWeapon;
using EWC.CustomWeapon.KillTracker;
using EWC.CustomWeapon.WeaponContext.Contexts;
using KillIndicatorFix;
using UnityEngine;

namespace EWC.Dependencies
{
    public static class KillAPIWrapper
    {
        public const string PLUGIN_GUID = "randomuserhi.KillIndicatorFix";
        public static readonly bool HasKIF;

        static KillAPIWrapper()
        {
            HasKIF = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static void TagEnemy(EnemyAgent enemy, ItemEquippable? item = null, Vector3? localHitPosition = null) => Kill.TagEnemy(enemy, item, localHitPosition);
    }
}
