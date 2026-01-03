using BepInEx.Unity.IL2CPP;
using EEC.CustomAbilities.Bleed;
using Player;
using System.Runtime.CompilerServices;

namespace EWC.Dependencies
{
    internal static class EECAPIWrapper
    {
        public const string PLUGIN_GUID = "GTFO.EECustomization";

        public readonly static bool HasEEC = false;

        static EECAPIWrapper()
        {
            HasEEC = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static void StopBleed(PlayerAgent agent)
        {
            if (HasEEC)
                StopBleed_Internal(agent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StopBleed_Internal(PlayerAgent agent) => BleedManager.StopBleed(agent);
    }
}
