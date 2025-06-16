using EWC.API;
using EWC.Attributes;
using EWC.CustomWeapon.Properties.Effects.Hit.Explosion.EEC_ExplosionFX;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using Player;
using SNetwork;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.Explosion
{
    internal static class ExplosionFXManager
    {
        private readonly static ExplosionFXSync _sync = new();
        private static FX_Pool _mineFXPool = null!;

        private static float _lastSoundTime = 0f;
        private static int _soundShotOverride = 0;
        private static Quaternion _mineFXRotation;

        public const float MaxGlowDuration = 50f;
        public const float MaxGlowIntensity = 512f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
            _mineFXPool = FX_Manager.GetEffectPool(AssetShards.AssetShardManager.GetLoadedAsset<GameObject>("Assets/AssetPrefabs/FX_Effects/FX_Tripmine.prefab"));
        }

        public static void DoExplosionFX(Vector3 position, Vector3 normal, Explosive eBase)
        {
            ExplosionFXData fxData = new() { position = position, propertyID = eBase.SyncPropertyID };
            fxData.normal.Value = normal;
            _sync.Send(fxData);
        }

        internal static void Internal_ReceiveExplosionFX(Vector3 position, Vector3 normal, Explosive eBase)
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
            if (Configuration.ShowExplosionEffect && eBase.Radius > 0)
            {
                if (eBase.EnableMineFX)
                {
                    _mineFXRotation.SetLookRotation(normal);
                    _mineFXPool.AquireEffect().Play(null, position, _mineFXRotation);
                }

                if (eBase.GlowDuration > 0 && eBase.GlowIntensity > 0)
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
        public LowResVector3_Normalized normal;
        public ushort propertyID;
    }
}
