﻿using BepInEx.Unity.IL2CPP;
using EWC.Utils.Log;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace EWC.Dependencies
{
    internal static class PDAPIWrapper
    {
        public const string PLUGIN_GUID = "MTFO.Extension.PartialBlocks";
        public readonly static bool HasPData = false;

        public static JsonConverter? PersistentIDConverter { get; private set; } = null;

        static PDAPIWrapper()
        {
            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(PLUGIN_GUID, out var info))
            {
                try
                {
                    var ddAsm = info?.Instance?.GetType()?.Assembly;
                    if (ddAsm is null)
                        throw new Exception("Assembly is Missing!");

                    var types = ddAsm.GetTypes();
                    var converterType = types.First(t => t.Name == "PersistentIDConverter");
                    if (converterType is null)
                        throw new Exception("Unable to Find PersistentIDConverter Class");

                    PersistentIDConverter = (JsonConverter)Activator.CreateInstance(converterType)!;
                    HasPData = true;
                }
                catch (Exception e)
                {
                    EWCLogger.Error($"Exception thrown while reading data from MTFO_Extension_PartialData:\n{e}");
                }
            }
        }
    }
}
