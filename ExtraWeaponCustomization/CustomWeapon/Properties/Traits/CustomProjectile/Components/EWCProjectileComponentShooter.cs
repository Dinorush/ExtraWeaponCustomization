using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileComponentShooter : EWCProjectileComponentBase
    {
        private ProjectileBase _projectile;
        private ProjectileTargeting? _targeting;
        private TrailRenderer? _trailRenderer;
        public ProjectileType Type;

        private Color _origGlowColor;
        private float _origGlowRange;
        private Vector3 _baseVelocity;

#pragma warning disable CS8618
        public EWCProjectileComponentShooter(IntPtr ptr) : base(ptr) { }
#pragma warning restore CS8618

        public override void Init(ushort ID, Vector3 position, Vector3 velocity, float accel, float accelExpo, float accelTime, float gravity, float scale, float lifetime, bool sendDestroy)
        {
            if (enabled) return;

            base.Init(ID, position, velocity, accel, accelExpo, accelTime, gravity, scale, lifetime, sendDestroy);
            _baseVelocity = velocity;

            _trailRenderer?.Clear();

            foreach (var effect in _projectile.m_effectsToStopEmittingOnImpact)
                effect.Play();
            foreach (var go in _projectile.m_toDestroyOnImpact)
                go.active = true;

            if (_targeting != null)
            {
                if (_targeting.m_light != null)
                    _targeting.m_light.enabled = true;
                if (_targeting.m_ricochetEffect != null)
                    _targeting.m_ricochetEffect.active = true;
            }
        }

        public void Init(ushort ID, Vector3 position, Vector3 velocity, float accel, float accelExpo, float accelTime, float gravity, float scale, bool trail, Color glowColor, float glowRange, float lifetime, bool sendDestroy)
        {
            if (enabled) return;

            Init(ID, position, velocity, accel, accelExpo, accelTime, gravity, scale, lifetime, sendDestroy);

            if (_trailRenderer != null)
                _trailRenderer.enabled = trail;

            if (_targeting != null && _targeting.m_light != null)
            {
                _targeting.m_light.Color = Approximately(glowColor, Color.black) ? _origGlowColor : glowColor;
                _targeting.m_light.Range = glowRange < 0f ? _origGlowRange : glowRange;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _projectile = GetComponent<ProjectileBase>();
            _targeting = _projectile.TryCast<ProjectileTargeting>();
            _projectile.OnFire(null);
            _projectile.m_soundPlayer.Stop();
            _projectile.enabled = false;
            _trailRenderer = GetComponentInChildren<TrailRenderer>();
            if (_targeting != null && _targeting.m_light != null)
            {
                _origGlowColor = _targeting.m_light.Color;
                _origGlowRange = _targeting.m_light.Range;
            }
        }

        protected override void Update()
        {
            _gravityVel += _gravity * Time.deltaTime;
            if (_accel != 1f)
            {
                _accelProgress = Math.Min(_accelProgress + Time.deltaTime / _accelTime, 1f);
                _velocity = _baseVelocity * Mathf.Lerp(1f, _accel, (float) Math.Pow(_accelProgress, _accelExpo));
            }
            else
                _velocity = _baseVelocity;
            _velocity.y -= _gravityVel;

            Vector3 velocity = _velocity * Time.deltaTime;
            base.Update();
            if (!enabled) return;

            _position += velocity;
            LerpVisualOffset();
            s_tempRot.SetLookRotation(_dirVisual);
            _projectile.m_soundPlayer.UpdatePosition(_positionVisual);
            _projectile.transform.SetPositionAndRotation(_positionVisual, s_tempRot);
        }

        public override void Die()
        {
            base.Die();
            foreach (var effect in _projectile.m_effectsToStopEmittingOnImpact)
                effect.Stop();
            foreach (var go in _projectile.m_toDestroyOnImpact)
                go.active = false;

            if (_targeting != null)
            {
                if (_targeting.m_light != null)
                    _targeting.m_light.enabled = false;
                if (_targeting.m_ricochetEffect != null)
                    _targeting.m_ricochetEffect.active = false;
            }

            EWCProjectileManager.Shooter.ReturnToPool(this);
        }

        private static bool Approximately(Color color1, Color color2)
        {
            return Mathf.Approximately(color1.a, color2.a)
                && Mathf.Approximately(color1.r, color2.r)
                && Mathf.Approximately(color1.g, color2.g)
                && Mathf.Approximately(color1.b, color2.b);
        }
    }
}
