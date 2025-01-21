using EWC.API;
using EWC.CustomWeapon.Properties.Effects.Hit.Explosion.EEC_ExplosionFX;
using EWC.Utils;
using Player;
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
            ExplosionFXData fxData = new() { position = position, propertyID = eBase.SyncPropertyID };
            _sync.Send(fxData, null, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveExplosionFX(Vector3 position, Explosive eBase)
        {
            // Sound
            if (Configuration.PlayExplosionSFX)
            {
                _soundShotOverride++;
                if (eBase.SoundID != 0 && (_soundShotOverride > Configuration.ExplosionSFXShotOverride || Clock.Time - _lastSoundTime > Configuration.ExplosionSFXCooldown))
                {
                    CellSound.Post(eBase.SoundID, position);
                    _soundShotOverride = 0;
                    _lastSoundTime = Clock.Time;
                }
            }

            // Lighting
            if (Configuration.ShowExplosionEffect && eBase.Radius > 0 && eBase.GlowDuration > 0 && eBase.GlowIntensity > 0)
            {
                ExplosionEffectPooling.TryDoEffect(new ExplosionEffectData()
                {
                    position = position,
                    flashColor = eBase.GlowColor,
                    intensity = eBase.GlowIntensity,
                    range = eBase.Radius,
                    duration = eBase.GlowDuration,
                    fadeDuration = eBase.GlowFadeDuration
                });
            }

            // Screen Shake
            if (Configuration.PlayExplosionShake && eBase.ScreenShakeIntensity > 0 && eBase.ScreenShakeDuration > 0)
            {
                float innerRadius = eBase.ScreenShakeInnerRadius > 0 ? eBase.ScreenShakeInnerRadius : eBase.InnerRadius;
                float radius = eBase.ScreenShakeRadius > 0 ? eBase.ScreenShakeRadius : eBase.Radius;
                PlayerAgent player = PlayerManager.GetLocalPlayerAgent();
                Vector3 diff = position - player.Position;
                float dist = diff.magnitude;
                if (dist < radius)
                {
                    float strength = dist.MapInverted(innerRadius, radius, eBase.ScreenShakeIntensity, 0f, eBase.Exponent);
                    player.FPSCamera.Shake(eBase.ScreenShakeDuration, strength, eBase.ScreenShakeFrequency, diff.normalized);
                }
            }

            ExplosionAPI.FireExplosionSpawnedCallback(position, eBase);
        }
    }

    public struct ExplosionFXData
    {
        public Vector3 position;
        public ushort propertyID;
    }
}
