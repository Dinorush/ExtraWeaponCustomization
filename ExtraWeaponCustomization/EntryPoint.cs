using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using EWC.Dependencies;
using GTFO.API;
using EWC.CustomWeapon;
using EWC.Utils.Log;
using System.Runtime.CompilerServices;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using EWC.Attributes;

namespace EWC;

[BepInPlugin("Dinorush." + MODNAME, MODNAME, "3.5.8")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MTFOAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MSAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(KillAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EXPAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ERDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EECAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(FSFAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ACAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public const string MODNAME = "ExtraWeaponCustomization";
    public static bool Loaded { get; private set; } = false;

    private IEnumerable<MethodInfo> _cleanupCallbacks = null!;
    private IEnumerable<MethodInfo> _enterCallbacks = null!;

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

        LevelAPI.OnLevelCleanup += LevelAPI_OnLevelCleanup;
        LevelAPI.OnEnterLevel += LevelAPI_OnLevelEnter;
        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;
        EWCLogger.Log("Loaded " + MODNAME);
    }

    private void LevelAPI_OnLevelCleanup()
    {
        foreach (var callback in _cleanupCallbacks)
            callback.Invoke(null, null);
    }

    private void LevelAPI_OnLevelEnter()
    {
        foreach (var callback in _enterCallbacks)
            callback.Invoke(null, null);
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        InvokeCallbacks<InvokeOnAssetLoadAttribute>();
        RuntimeHelpers.RunClassConstructor(typeof(CustomWeaponManager).TypeHandle);
    }

    private void CacheFrequentCallbacks()
    {
        Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
        var methods = typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods);
        _cleanupCallbacks = from method in methods
                            where method.GetCustomAttribute<InvokeOnCleanupAttribute>() != null
                            where method.IsStatic
                            select method;
        _enterCallbacks = from method in methods
                          where method.GetCustomAttribute<InvokeOnEnterAttribute>() != null
                          where method.IsStatic
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