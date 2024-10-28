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
        public static bool ShowExplosionEffect { get; set; } = true;
        public static bool PlayExplosionSFX { get; set; } = true;
        public static float ExplosionSFXCooldown { get; set; } = 0.08f;
        public static int ExplosionSFXShotOverride { get; set; } = 8;

        public static float AutoAimTickDelay { get; set; } = 0.1f;

        public static float HomingTickDelay { get; set; } = 0.1f;

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
            configFile.Reload();
            string section = "Auto Aim Settings";
            AutoAimTickDelay = (float)configFile[section, "Search Cooldown"].BoxedValue;

            section = "Explosion Settings";
            ShowExplosionEffect = (bool)configFile[section, "Show Effect"].BoxedValue;
            PlayExplosionSFX = (bool)configFile[section, "Play Sound"].BoxedValue;
            ExplosionSFXCooldown = (float)configFile[section, "SFX Cooldown"].BoxedValue;
            ExplosionSFXShotOverride = (int)configFile[section, "Shots to Override SFX Cooldown"].BoxedValue;

            section = "Projectile Settings";
            HomingTickDelay = (float)configFile[section, "Homing Search Cooldown"].BoxedValue;

            CheckAndRefreshTemplate();
        }

        private static void BindAll(ConfigFile config)
        {
            string section = "Auto Aim Settings";
            AutoAimTickDelay = config.Bind(section, "Search Cooldown", AutoAimTickDelay, "Time between attempted searches to acquire targets.").Value;

            section = "Explosion Settings";
            ShowExplosionEffect = config.Bind(section, "Show Effect", ShowExplosionEffect, "Enables explosion visual FX.").Value;
            PlayExplosionSFX = config.Bind(section, "Play Sound", PlayExplosionSFX, "Enables explosion sound FX.").Value;
            ExplosionSFXCooldown = config.Bind(section, "SFX Cooldown", ExplosionSFXCooldown, "Minimum time between explosion sound effects, to prevent obnoxiously loud sounds.").Value;
            ExplosionSFXShotOverride = config.Bind(section, "Shots to Override SFX Cooldown", ExplosionSFXShotOverride, "Amount of shots fired before another explosion sound effect is forced, regardless of cooldown.\nSmaller numbers let fast-firing weapons and shotguns make more sounds in a short span of time.").Value;

            section = "Projectile Settings";
            HomingTickDelay = config.Bind(section, "Homing Search Cooldown", HomingTickDelay, "Minimum time between attempted searches to acquire a new target.").Value;

            section = "Tools";
            ForceCreateTemplate = config.Bind(section, "Force Create Template", false, "Creates the template file again.");
        }

        private static void CheckAndRefreshTemplate()
        {
            if (ForceCreateTemplate.Value == true)
            {
                ForceCreateTemplate.Value = false;
                CustomWeaponManager.Current.CreateTemplate();
                configFile.Save();
            }
        }
    }
}
