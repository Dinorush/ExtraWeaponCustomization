using BepInEx.Unity.IL2CPP;
using EWC.Utils.Log;
using Player;
using System;
using System.Linq;
using System.Reflection;

namespace EWC.Dependencies
{
    internal static class EECAPIWrapper
    {
        public const string PLUGIN_GUID = "GTFO.EECustomization";
        public readonly static bool HasEEC = false;

        public static Action<PlayerAgent> StopBleed { get; private set; } = (agent) => { };

        static EECAPIWrapper()
        {
            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(PLUGIN_GUID, out var info))
            {
                try
                {
                    var ddAsm = info?.Instance?.GetType()?.Assembly;
                    if (ddAsm is null)
                        throw new Exception("Assembly is Missing!");

                    var types = ddAsm.GetTypes();
                    var bleedManager = types.First(t => t.Name == "BleedManager");
                    if (bleedManager is null)
                        throw new Exception("Unable to find BleedManager Class");

                    var method = bleedManager.GetMethod("StopBleed", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(PlayerAgent) });
                    if (method is null)
                        throw new Exception("Unable to find StopBleed method!");

                    StopBleed = (Action<PlayerAgent>) method.CreateDelegate(typeof(Action<PlayerAgent>));
                    HasEEC = true;
                }
                catch (Exception e)
                {
                    EWCLogger.Error($"Exception thrown while reading data from EEC:\n{e}");
                }
            }
        }
    }
}
