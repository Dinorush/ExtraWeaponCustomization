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

        public static void GetETCData(uint archetypeID, out bool backDamage, out float friendlyMulti)
        {
            if (hasETC)
                ETC_GetData(archetypeID, out backDamage, out friendlyMulti);
            else
            {
                backDamage = false;
                friendlyMulti = 1f;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ETC_GetData(uint archetypeID, out bool backDamage, out float friendlyMulti)
        {
            var data = ToolDataManager.GetArchData<SentryData>(archetypeID);
            if (data != null)
            {
                backDamage = data.BackDamage;
                friendlyMulti = data.FriendlyDamageMulti;
            }
            else
            {
                backDamage = false;
                friendlyMulti = 1f;
            }
        }
    }
}
