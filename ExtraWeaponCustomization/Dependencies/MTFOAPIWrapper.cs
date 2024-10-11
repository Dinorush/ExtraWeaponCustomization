using MTFO.API;

namespace EWC.Dependencies
{
    internal static class MTFOAPIWrapper
    {
        public const string PLUGIN_GUID = "com.dak.MTFO";
        
        public static string GameDataPath => MTFOPathAPI.RundownPath;
        public static string CustomPath => MTFOPathAPI.CustomPath;
        public static bool HasCustomContent => MTFOPathAPI.HasRundownPath;
    }
}
