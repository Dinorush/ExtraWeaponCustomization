using EWC.Attributes;
using EWC.Utils.Extensions;
using Il2CppInterop.Runtime.Injection;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.Explosion.EEC_ExplosionFX.Handlers
{
    internal sealed class ExplosionEffectHandler : MonoBehaviour
    {
        public Action? EffectDoneOnce;

        private EffectLight? _light;
        private Timer _timer;
        private bool _effectOnGoing = false;

        private float _initIntensity;
        private float _fadeDuration;

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ExplosionEffectHandler>();
        }

        internal void DoEffect(ExplosionEffectData data)
        {
            transform.position = data.position;

            if (_light == null)
            {
                _light = gameObject.GetComponent<EffectLight>();
                _light.Setup();
            }

            _light.UpdateVisibility(true);
            _light.Color = data.flashColor;
            _light.Range = data.range;
            _light.Intensity = _initIntensity = data.intensity;
            _timer.Reset(data.duration);
            _fadeDuration = data.fadeDuration;
            _effectOnGoing = true;
        }

        private void Update()
        {
            if (_effectOnGoing && _fadeDuration > 0 && _light != null)
                _light.Intensity = _timer.PassedTime.Map(_timer.Duration - _fadeDuration, _timer.Duration, _initIntensity, 0f);
        }

        private void FixedUpdate()
        {
            if (_effectOnGoing && _timer.TickAndCheckDone())
                OnDone();
        }

        private void OnDone()
        {
            if (_light != null)
            {
                _light.UpdateVisibility(false);
            }

            EffectDoneOnce?.Invoke();
            EffectDoneOnce = null;
            _light = null;
            _effectOnGoing = false;
        }
    }
}