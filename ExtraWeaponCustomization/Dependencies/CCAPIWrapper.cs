using BepInEx.Unity.IL2CPP;

namespace EWC.Dependencies
{
    internal static class CCAPIWrapper
    {
        public const string PLUGIN_GUID = "CConsole";
        public static readonly bool HasCC;

        static CCAPIWrapper()
        {
            HasCC = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }
    }
}
