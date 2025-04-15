using BepInEx.Unity.IL2CPP;
using Enemies;
using KillIndicatorFix;
using System.Runtime.CompilerServices;
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

        public static void TagEnemy(EnemyAgent enemy, ItemEquippable? item = null, Vector3? localHitPosition = null)
        {
            if (HasKIF)
                TagEnemy_Internal(enemy, item, localHitPosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TagEnemy_Internal(EnemyAgent enemy, ItemEquippable? item, Vector3? localHitPosition) => Kill.TagEnemy(enemy, item, localHitPosition);
    }
}
