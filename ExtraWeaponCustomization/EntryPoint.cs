using ExtraWeaponCustomization.Patches;
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

namespace ExtraWeaponCustomization;

[BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.4.3")]
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

        Harmony harmonyInstance = new(MODNAME);
        harmonyInstance.PatchAll(typeof(PlayerInventoryPatches));
        harmonyInstance.PatchAll(typeof(WeaponArchetypePatches));
        harmonyInstance.PatchAll(typeof(WeaponPatches));
        harmonyInstance.PatchAll(typeof(WeaponRayPatches));
        harmonyInstance.PatchAll(typeof(WeaponRecoilPatches));
        harmonyInstance.PatchAll(typeof(PlayerLocalPatches));

        Configuration.Init();
        LevelAPI.OnEnterLevel += LevelAPI_OnEnterLevel;
        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;
        EWCLogger.Log("Loaded " + MODNAME);
    }

    private void LevelAPI_OnEnterLevel()
    {
        CustomWeaponManager.Current.ResetCWCs();
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        ClassInjector.RegisterTypeInIl2Cpp<ExplosionEffectHandler>();
        ClassInjector.RegisterTypeInIl2Cpp<CustomWeaponComponent>();
        ExplosionManager.Init();
        DOTDamageManager.Init();
        HealManager.Init();
        KillAPIWrapper.Init();
        CustomWeaponManager.Current.GetCustomWeaponData(0); // Just want to make it get custom weapon data on startup, need to call something
    }
}