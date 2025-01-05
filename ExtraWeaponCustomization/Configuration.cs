using BepInEx.Configuration;
using BepInEx;
using System.IO;
using GTFO.API.Utilities;
using EWC.CustomWeapon;

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

            section = "Projectile Settings";
            _homingTickDelay = configFile.Bind(section, "Homing Search Cooldown", 0.1f, "Minimum time between attempted searches to acquire a new target.");

            section = "Tools";
            ForceCreateTemplate = configFile.Bind(section, "Force Create Template", false, "Creates the template file again.");
        }

        internal static void Init()
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
                CustomWeaponManager.Current.CreateTemplate();
                configFile.Save();
            }
        }
    }
}
