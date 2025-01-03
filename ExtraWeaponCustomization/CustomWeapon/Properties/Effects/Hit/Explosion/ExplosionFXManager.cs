﻿using EWC.CustomWeapon.Properties.Effects.Hit.Explosion.EEC_ExplosionFX;
using EWC.Networking.Structs;
using SNetwork;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.Explosion
{
    internal static class ExplosionFXManager
    {
        private readonly static ExplosionFXSync _sync = new();

        private static float _lastSoundTime = 0f;
        private static int _soundShotOverride = 0;

        public const float MaxGlowDuration = 50f;
        public const float MaxGlowIntensity = 512f;

        internal static void Init()
        {
            _sync.Setup();
            ExplosionEffectPooling.Initialize();
        }

        public static void DoExplosionFX(Vector3 position, Explosive eBase)
        {
            ExplosionFXData fxData = new() { position = position, soundID = eBase.SoundID, color = eBase.GlowColor };
            fxData.intensity.Set(eBase.GlowIntensity, MaxGlowIntensity);
            fxData.radius.Set(eBase.Radius, ExplosionManager.MaxRadius);
            fxData.duration.Set(eBase.GlowDuration, MaxGlowDuration);
            fxData.fadeDuration.Set(eBase.GlowFadeDuration, MaxGlowDuration);
            _sync.Send(fxData, null, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveExplosionFX(Vector3 position, float radius, uint soundID, Color color, float intensity, float duration, float fadeDuration)
        {
            // Sound
            if (Configuration.PlayExplosionSFX)
            {
                _soundShotOverride++;
                if (_soundShotOverride > Configuration.ExplosionSFXShotOverride || Clock.Time - _lastSoundTime > Configuration.ExplosionSFXCooldown)
                {
                    CellSound.Post(soundID, position);
                    _soundShotOverride = 0;
                    _lastSoundTime = Clock.Time;
                }
            }

            // Lighting
            if (Configuration.ShowExplosionEffect)
            {
                ExplosionEffectPooling.TryDoEffect(new ExplosionEffectData()
                {
                    position = position,
                    flashColor = color,
                    intensity = intensity,
                    range = radius,
                    duration = duration,
                    fadeDuration = fadeDuration
                });
            }
        }
    }

    public struct ExplosionFXData
    {
        public Vector3 position;
        public UFloat16 radius;
        public uint soundID;
        public LowResColor color;
        public UFloat16 intensity;
        public UFloat16 duration;
        public UFloat16 fadeDuration;
    }
}
