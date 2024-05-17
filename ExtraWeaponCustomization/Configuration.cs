using BepInEx.Configuration;
using BepInEx;
using UnityEngine;
using System.IO;
using GTFO.API.Utilities;

namespace ExtraWeaponCustomization
{
    internal static class Configuration
    {
        public static bool ShowExplosionEffect { get; set; } = true;
        public static float ExplosionSFXCooldown { get; set; } = 0.08f;
        public static int ExplosionSFXShotOverride { get; set; } = 8;

        private readonly static ConfigFile configFile;

        static Configuration()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            BindAll(configFile);
        }

        internal static void Init()
        {
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private static void OnFileChanged(LiveEditEventArgs _)
        {
            string section = "Explosion Settings";
            configFile.Reload();
            ShowExplosionEffect = (bool)configFile[section, "Show Effect"].BoxedValue;
            ExplosionSFXCooldown = (float)configFile[section, "SFX Cooldown"].BoxedValue;
            ExplosionSFXShotOverride = (int)configFile[section, "Shots to Override SFX Cooldown"].BoxedValue;
        }

        private static void BindAll(ConfigFile config)
        {
            string section = "Explosion Settings";
            ShowExplosionEffect = config.Bind(section, "Show Effect", ShowExplosionEffect, "Enables explosion visual FX.").Value;
            ExplosionSFXCooldown = config.Bind(section, "SFX Cooldown", ExplosionSFXCooldown, "Minimum cooldown between explosion sound effects, to prevent obnoxiously loud sounds.").Value;
            ExplosionSFXShotOverride = config.Bind(section, "Shots to Override SFX Cooldown", ExplosionSFXShotOverride, "Amount of shots fired before another explosion visual effect is forced, regardless of cooldown.\nSmaller numbers let fast-firing weapons and shotguns spawn more effects in a short span of time.").Value;
        }
    }
}
