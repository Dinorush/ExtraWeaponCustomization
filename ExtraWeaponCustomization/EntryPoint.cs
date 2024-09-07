using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ExtraWeaponCustomization.Utils;
using ExtraWeaponCustomization.Dependencies;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.EEC_Explosion.Handlers;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Heal;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.FireRate;

namespace ExtraWeaponCustomization;

[BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.8.3")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MTFOUtil.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MTFOPartialDataUtil.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(KillAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(EXPAPIWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public const string MODNAME = "ExtraWeaponCustomization";

    public override void Load()
    {
        EWCLogger.Log("Loading " + MODNAME);

        new Harmony(MODNAME).PatchAll();
        Configuration.Init();
        LevelAPI.OnLevelCleanup += LevelAPI_OnLevelCleanup;
        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;
        EWCLogger.Log("Loaded " + MODNAME);
    }

    private void LevelAPI_OnLevelCleanup()
    {
        CustomWeaponManager.Current.ResetCWCs();
        EWCProjectileManager.Reset();
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        ClassInjector.RegisterTypeInIl2Cpp<ExplosionEffectHandler>();
        ClassInjector.RegisterTypeInIl2Cpp<CustomWeaponComponent>();
        ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentBase>();
        ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentShooter>();
        ExplosionManager.Init();
        DOTDamageManager.Init();
        HealManager.Init();
        FireRateModManager.Init();
        KillAPIWrapper.Init();
        EWCProjectileManager.Init();
        CustomWeaponManager.Current.GetCustomWeaponData(0); // Just want to make it get custom weapon data on startup, need to call something
    }
}