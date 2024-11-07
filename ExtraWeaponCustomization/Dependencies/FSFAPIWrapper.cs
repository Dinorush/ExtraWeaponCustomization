using BepInEx.Unity.IL2CPP;

namespace EWC.Dependencies
{
    internal static class FSFAPIWrapper
    {
        public const string PLUGIN_GUID = "Dinorush.FlickShotFix";

        public static readonly bool hasFSF;

        static FSFAPIWrapper()
        {
            hasFSF = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }
    }
}
