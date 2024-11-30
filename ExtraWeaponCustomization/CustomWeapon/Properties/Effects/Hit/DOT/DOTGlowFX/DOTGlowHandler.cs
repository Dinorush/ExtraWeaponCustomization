using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT.DOTGlowFX
{
    internal sealed class DOTGlowHandler : MonoBehaviour
    {
        public Action? EffectDoneOnce;

        private EffectLight? _light;
        private bool _effectOnGoing = false;

        private readonly Queue<(float intensity, float endTime)> _endTimes = new();
        private DamageOverTime? _data;
        private IDamageable? _damBase;
        private Transform? _targetTransform;
        private Vector3 _targetPos;

        internal void DoEffect(DamageOverTime data, IDamageable damBase, Transform target, float mod)
        {
            _data = data;
            _damBase = damBase;
            _targetTransform = target;

            transform.position = TargetPosition;

            if (_light == null)
            {
                _light = gameObject.GetComponent<EffectLight>();
                _light.Setup();
            }

            _light.UpdateVisibility(true);
            _light.Color = data.GlowColor;
            _light.Range = data.GlowRange;
            AddInstance(mod);
            _effectOnGoing = true;
        }

        internal void AddInstance(float mod)
        {
            float intensity = _data!.GlowIntensity * mod;
            _light!.Intensity += intensity;
            _endTimes.Enqueue((intensity, _data.Duration + Clock.Time));
            if (_data.StackLimit > 0 && _endTimes.Count > _data.StackLimit)
            {
                (intensity, _) = _endTimes.Dequeue();
                _light!.Intensity -= intensity;
            }
        }

        internal void ForceEnd() => OnDone();

        private Vector3 TargetPosition => _targetTransform != null ? _targetTransform.position : _targetPos;

        private void Update()
        {
            if (_damBase == null || _damBase.GetHealthRel() <= 0)
                OnDone();
            else
                transform.position = TargetPosition;
        }

        private void FixedUpdate()
        {
            while (_endTimes.TryPeek(out (float intensity, float endTime) pair) && Clock.Time >= pair.endTime)
            {
                _light!.Intensity -= pair.intensity;
                _endTimes.Dequeue();
            }

            if (_effectOnGoing && _endTimes.Count == 0)
                OnDone();
        }

        private void OnDone()
        {
            if (_light != null)
            {
                _light.UpdateVisibility(false);
            }

            EffectDoneOnce?.Invoke();
            _endTimes.Clear();
            EffectDoneOnce = null;
            _light = null;
            _effectOnGoing = false;
        }
    }
}