using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using EWC.Dependencies;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using EWC.CustomWeapon;
using EWC.CustomWeapon.Properties.Effects.EEC_Explosion.Handlers;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using EWC.CustomWeapon.Properties.Effects.Heal;
using EWC.Utils.Log;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils;
using EWC.Patches.Native;

namespace EWC;

[BepInPlugin("Dinorush." + MODNAME, MODNAME, "2.4.2")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MTFOAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(KillAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(PDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EXPAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ERDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public const string MODNAME = "ExtraWeaponCustomization";

    public override void Load()
    {
        EWCLogger.Log("Loading " + MODNAME);
        if (!MTFOAPIWrapper.HasCustomContent)
        {
            EWCLogger.Error("No MTFO datablocks detected. Not loading EWC...");
            return;
        }

        new Harmony(MODNAME).PatchAll();
        EnemyDetectionPatches.ApplyNativePatch();
        Configuration.Init();
        LevelAPI.OnLevelCleanup += LevelAPI_OnLevelCleanup;
        LevelAPI.OnEnterLevel += LevelAPI_OnLevelEnter;
        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;
        EWCLogger.Log("Loaded " + MODNAME);
    }

    private void LevelAPI_OnLevelCleanup()
    {
        CustomWeaponManager.Current.ResetCWCs(false);
        EWCProjectileManager.Reset();
    }

    private void LevelAPI_OnLevelEnter()
    {
        CustomWeaponManager.Current.ActivateCWCs();
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        ClassInjector.RegisterTypeInIl2Cpp<ExplosionEffectHandler>();
        ClassInjector.RegisterTypeInIl2Cpp<CustomWeaponComponent>();
        ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentBase>();
        ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentShooter>();

        LayerUtil.Init();
        ExplosionManager.Init();
        DOTDamageManager.Init();
        HealManager.Init();
        TriggerManager.Init();
        KillAPIWrapper.Init();
        EWCProjectileManager.Init();
        CustomWeaponManager.Current.GetCustomGunData(0); // Just want to make it get custom weapon data on startup, need to call something
    }
}