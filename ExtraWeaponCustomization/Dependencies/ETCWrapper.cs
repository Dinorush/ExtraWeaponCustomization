using BepInEx.Unity.IL2CPP;
using ExtraToolCustomization.ToolData;
using System.Runtime.CompilerServices;

namespace EWC.Dependencies
{
    internal static class ETCWrapper
    {
        public const string PLUGIN_GUID = "Dinorush.ExtraToolCustomization";

        public static readonly bool hasETC;

        static ETCWrapper()
        {
            hasETC = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static bool CanDoBackDamage(uint archetypeID) => hasETC && ETC_CanDoBackDamage(archetypeID);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ETC_CanDoBackDamage(uint archetypeID) => ToolDataManager.GetArchData<SentryData>(archetypeID)?.BackDamage == true;
    }
}
