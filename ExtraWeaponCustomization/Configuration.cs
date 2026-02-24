using BepInEx.Configuration;
using BepInEx;
using System.IO;
using GTFO.API.Utilities;
using EWC.CustomWeapon;
using EWC.Attributes;
using UnityEngine;

namespace EWC
{
    internal static class Configuration
    {
        private static ConfigEntry<bool> ForceCreateTemplate { get; set; }

        private readonly static ConfigEntry<bool> _showExplosionEffect;
        public static bool ShowExplosionEffect => _showExplosionEffect.Value;
        private readonly static ConfigEntry<bool> _playExplosionSFX;
        public static bool PlayExplosionSFX => _playExplosionSFX.Value;
        private readonly static ConfigEntry<float> _explosionSFXCooldown;
        public static float ExplosionSFXCooldown => _explosionSFXCooldown.Value;
        private readonly static ConfigEntry<int> _explosionSFXShotOverride;
        public static int ExplosionSFXShotOverride => _explosionSFXShotOverride.Value;
        private readonly static ConfigEntry<bool> _playExplosionShake;
        public static bool PlayExplosionShake => _playExplosionShake.Value;

        private readonly static ConfigEntry<KeyCode> _keybind1;
        public static KeyCode Keybind1 => _keybind1.Value;
        private readonly static ConfigEntry<KeyCode> _keybind2;
        public static KeyCode Keybind2 => _keybind2.Value;
        private readonly static ConfigEntry<KeyCode> _keybind3;
        public static KeyCode Keybind3 => _keybind3.Value;
        private readonly static ConfigEntry<KeyCode> _keybind4;
        public static KeyCode Keybind4 => _keybind4.Value;

        private readonly static ConfigEntry<float> _autoAimTickDelay;
        public static float AutoAimTickDelay => _autoAimTickDelay.Value;

        private readonly static ConfigEntry<float> _homingTickDelay;
        public static float HomingTickDelay => _homingTickDelay.Value;

        private readonly static ConfigFile configFile;

        static Configuration()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            string section = "Auto Aim Settings";
            _autoAimTickDelay = configFile.Bind(section, "Search Cooldown", 0.1f, "Time between attempted searches to acquire targets.");

            section = "Explosion Settings";
            _showExplosionEffect = configFile.Bind(section, "Show Effect", true, "Enables explosion visual FX.");
            _playExplosionSFX = configFile.Bind(section, "Play Sound", true, "Enables explosion sound FX.");
            _explosionSFXCooldown = configFile.Bind(section, "SFX Cooldown", 0.08f, "Minimum time between explosion sound effects, to prevent obnoxiously loud sounds.");
            _explosionSFXShotOverride = configFile.Bind(section, "Shots to Override SFX Cooldown", 8, "Amount of shots fired before another explosion sound effect is forced, regardless of cooldown.\nSmaller numbers let fast-firing weapons and shotguns make more sounds in a short span of time.");
            _playExplosionShake = configFile.Bind(section, "Play Screen Shake", true, "Enables explosion screen shake. Doesn't bypass the global screen shake settings modifier.");

            section = "Keybind Settings";
            _keybind1 = configFile.Bind(section, "Keybind 1", KeyCode.V, "A mappable keybind used to trigger properties.\nIf changed while in a level, requires re-drop/reloading weapon properties to update.");
            _keybind2 = configFile.Bind(section, "Keybind 2", KeyCode.B, "A mappable keybind used to trigger properties.\nIf changed while in a level, requires re-drop/reloading weapon properties to update.");
            _keybind3 = configFile.Bind(section, "Keybind 3", KeyCode.X, "A mappable keybind used to trigger properties.\nIf changed while in a level, requires re-drop/reloading weapon properties to update.");
            _keybind4 = configFile.Bind(section, "Keybind 4", KeyCode.Z, "A mappable keybind used to trigger properties.\nIf changed while in a level, requires re-drop/reloading weapon properties to update.");

            section = "Projectile Settings";
            _homingTickDelay = configFile.Bind(section, "Homing Search Cooldown", 0.1f, "Minimum time between attempted searches to acquire a new target.");

            section = "Tools";
            ForceCreateTemplate = configFile.Bind(section, "Force Create Template", false, "Creates the template file again.");

            CheckAndRefreshTemplate();
        }

        [InvokeOnLoad]
        private static void Init()
        {
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private static void OnFileChanged(LiveEditEventArgs _)
        {
            configFile.Reload();
            CheckAndRefreshTemplate();
        }

        private static void CheckAndRefreshTemplate()
        {
            if (ForceCreateTemplate.Value)
            {
                ForceCreateTemplate.Value = false;
                CustomDataManager.Current.CreateTemplate();
                configFile.Save();
            }
        }
    }
}
