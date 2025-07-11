using BepInEx.Unity.IL2CPP;
using Enemies;
using System.Runtime.CompilerServices;

namespace EWC.Dependencies
{
    internal static class DamageSyncWrapper
    {
        public const string PLUGIN_GUID = "randomuserhi.DamageSync";

        public static readonly bool hasDS;

        static DamageSyncWrapper()
        {
            hasDS = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static void RunDamageSync(EnemyAgent enemy, int limbID)
        {
            if (hasDS)
                RunDamageSync_Internal(enemy, limbID);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunDamageSync_Internal(EnemyAgent enemy, int limbID) => DamSync.DamageSync.Sync(enemy, limbID);
    }
}
