using BepInEx;
using BepInEx.Unity.IL2CPP;
using EWC.Attributes;
using EWC.CustomWeapon;
using EWC.Dependencies;
using GTFO.API;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EWC;

[BepInPlugin("Dinorush." + MODNAME, MODNAME, "4.0.0")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MTFOAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MSAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(AmorLibWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(KillAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(DamageSyncWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EXPAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ERDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EECAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(FSFAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ACAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(MSCWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ETCWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public const string MODNAME = "ExtraWeaponCustomization";
    public static bool Loaded { get; private set; } = false;

    private IEnumerable<MethodInfo> _cleanupCallbacks = null!;
    private IEnumerable<MethodInfo> _checkpointCallbacks = null!;
    private IEnumerable<MethodInfo> _enterCallbacks = null!;
    private IEnumerable<MethodInfo> _buildDoneCallbacks = null!;

    public override void Load()
    {
        if (!MTFOAPIWrapper.HasCustomContent)
        {
            EWCLogger.Error("No MTFO datablocks detected. Not loading EWC...");
            return;
        }
        Loaded = true;

        var harmony = new Harmony(MODNAME);
        harmony.PatchAll();

        CacheFrequentCallbacks();
        InvokeCallbacks<InvokeOnLoadAttribute>();

        Patches.SNet.SyncManagerPatches.OnCheckpointReload += RunFrequentCallback(_checkpointCallbacks);
        LevelAPI.OnLevelCleanup += RunFrequentCallback(_cleanupCallbacks);
        LevelAPI.OnEnterLevel += RunFrequentCallback(_enterCallbacks);
        LevelAPI.OnBuildDone += RunFrequentCallback(_buildDoneCallbacks);
        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;
        EWCLogger.Log("Loaded " + MODNAME);
    }

    private static Action RunFrequentCallback(IEnumerable<MethodInfo> callbacks)
    {
        return () =>
        {
            foreach (var callback in callbacks)
                callback.Invoke(null, null);
        };
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        InvokeCallbacks<InvokeOnAssetLoadAttribute>();
        RuntimeHelpers.RunClassConstructor(typeof(CustomDataManager).TypeHandle);
    }

    private void CacheFrequentCallbacks()
    {
        Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
        var methods = typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods).Where(method => method.IsStatic);
        var cleanups = from method in methods
                            let attr = method.GetCustomAttribute<InvokeOnCleanupAttribute>()
                            where attr != null
                            select new { Method = method, Attribute = attr };

        _cleanupCallbacks = from pair in cleanups select pair.Method;

        _checkpointCallbacks = from pair in cleanups
                               where pair.Attribute.OnCheckpoint
                               select pair.Method;

        _enterCallbacks = from method in methods
                          where method.GetCustomAttribute<InvokeOnEnterAttribute>() != null
                          select method;

        _buildDoneCallbacks = from method in methods
                               where method.GetCustomAttribute<InvokeOnBuildDoneAttribute>() != null
                               select method;
    }

    private void InvokeCallbacks<T>() where T : Attribute
    {
        Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
        IEnumerable<MethodInfo> enumerable = from method in typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods)
                                             where method.GetCustomAttribute<T>() != null
                                             where method.IsStatic
                                             select method;
        foreach (MethodInfo item in enumerable)
            item.Invoke(null, null);
    }
}