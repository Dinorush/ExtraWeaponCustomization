using BepInEx.Unity.IL2CPP;
using EEC.CustomAbilities.Bleed;
using EEC.CustomAbilities.EMP;
using EWC.Utils.Extensions;
using Player;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

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

        public static bool TryGetEMPController(MonoBehaviour comp, [MaybeNullWhen(false)] out MonoBehaviour controller)
        {
            if (HasEEC)
                return TryGetEMPComponent_Internal(comp, out controller);
            controller = null;
            return false;
        }

        public static void AddEMPController(MonoBehaviour comp)
        {
            if (HasEEC)
                AddEMPComponent_Internal(comp);
        }

        public static void RemoveEMPController(MonoBehaviour comp)
        {
            if (HasEEC)
                RemoveEMPComponent_Internal(comp);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StopBleed_Internal(PlayerAgent agent) => BleedManager.StopBleed(agent);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryGetEMPComponent_Internal(MonoBehaviour comp, [MaybeNullWhen(false)] out MonoBehaviour controller)
        {
            controller = comp.GetComponent<EMPController>();
            return controller != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddEMPComponent_Internal(MonoBehaviour comp)
        {
            if (comp.TryCastOut<EMPController>(out var controller))
            {
                controller.enabled = true;
                EMPManager.AddTarget(controller);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RemoveEMPComponent_Internal(MonoBehaviour comp)
        {
            if (comp.TryCastOut<EMPController>(out var controller))
            {
                controller.enabled = false;
                EMPManager.RemoveTarget(controller);
            }
        }
    }
}
