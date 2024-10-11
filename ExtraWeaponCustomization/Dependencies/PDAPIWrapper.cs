using BepInEx.Unity.IL2CPP;
using MTFO.Ext.PartialData.JsonConverters;
using System.Text.Json.Serialization;

namespace EWC.Dependencies
{
    internal static class PDAPIWrapper
    {
        public const string PLUGIN_GUID = "MTFO.Extension.PartialBlocks";
        public readonly static bool HasPData = false;

        public static JsonConverter? PersistentIDConverter { get; private set; } = null;
        public static JsonConverter? LocalizedTextConverter { get; private set; } = null;

        static PDAPIWrapper()
        {
            HasPData = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
            if (HasPData)
                PData_CreateConverters();
        }

        private static void PData_CreateConverters()
        {
            PersistentIDConverter = new PersistentIDConverter();
            LocalizedTextConverter = new GTFO.API.JSON.Converters.LocalizedTextConverter();
        }
    }
}
