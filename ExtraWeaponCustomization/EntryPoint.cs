using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using EWC.Dependencies;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using EWC.CustomWeapon;
using EWC.Utils.Log;
using EWC.Utils;
using EWC.Patches.Native;
using EWC.Patches.Player;
using System.Runtime.CompilerServices;
using EWC.CustomWeapon.Properties.Effects.Hit.DOT.DOTGlowFX;
using EWC.CustomWeapon.Properties.Effects.Hit.Explosion.EEC_ExplosionFX.Handlers;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;

namespace EWC;

[BepInPlugin("Dinorush." + MODNAME, MODNAME, "3.0.4")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MTFOAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(KillAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EXPAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ERDAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(EECAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(FSFAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(CCAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ACAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public const string MODNAME = "ExtraWeaponCustomization";
    public static bool Loaded { get; private set; } = false;

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
        EnemyDetectionPatches.ApplyNativePatch();
        if (!CCAPIWrapper.HasCC)
            harmony.PatchAll(typeof(PlayerDamagePatches));

        Configuration.Init();
        LevelAPI.OnLevelCleanup += LevelAPI_OnLevelCleanup;
        LevelAPI.OnEnterLevel += LevelAPI_OnLevelEnter;
        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;
        EWCLogger.Log("Loaded " + MODNAME);
    }

    private void LevelAPI_OnLevelCleanup()
    {
        CustomWeaponManager.Current.ResetCWCs(false);
        CustomWeapon.Properties.Traits.CustomProjectile.Managers.EWCProjectileManager.Reset();
        CustomWeapon.Properties.Effects.Hit.DOT.DOTDamageManager.Reset();
    }

    private void LevelAPI_OnLevelEnter()
    {
        CustomWeaponManager.Current.ActivateCWCs();
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        ClassInjector.RegisterTypeInIl2Cpp<DOTGlowHandler>();
        ClassInjector.RegisterTypeInIl2Cpp<ExplosionEffectHandler>();
        ClassInjector.RegisterTypeInIl2Cpp<CustomWeaponComponent>();
        ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentBase>();
        ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentShooter>();

        LayerUtil.Init();
        CustomWeapon.Properties.Effects.Hit.Explosion.ExplosionManager.Init();
        CustomWeapon.Properties.Effects.Hit.DOT.DOTDamageManager.Init();
        CustomWeapon.Properties.Effects.Hit.CustomFoam.FoamActionManager.Init();
        CustomWeapon.Properties.Effects.Heal.HealManager.Init();
        CustomWeapon.Properties.Effects.Triggers.TriggerManager.Init();
        CustomWeapon.Properties.Traits.CustomProjectile.Managers.EWCProjectileManager.Init();
        RuntimeHelpers.RunClassConstructor(typeof(CustomWeaponManager).TypeHandle);
    }
}