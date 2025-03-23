using BepInEx.Unity.IL2CPP;
using AccurateCrosshair.API;
using System.Runtime.CompilerServices;

namespace EWC.Dependencies
{
    internal static class ACAPIWrapper
    {
        public const string PLUGIN_GUID = "Dinorush.AccurateCrosshair";

        public static readonly bool hasAC;

        static ACAPIWrapper()
        {
            hasAC = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static void UpdateCrosshairSpread(float mod)
        {
            if (hasAC)
                UpdateCrosshairSpread_AC(mod);
        }

        public static void ResetCrosshairSpread()
        {
            if (hasAC)
                ResetCrosshairSpread_AC();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UpdateCrosshairSpread_AC(float mod) => SpreadAPI.SetModifier(EntryPoint.MODNAME, mod);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ResetCrosshairSpread_AC() => SpreadAPI.RemoveModifier(EntryPoint.MODNAME);

    }
}
