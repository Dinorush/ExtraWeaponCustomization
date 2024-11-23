using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using Il2CppInterop.Runtime.Attributes;
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

        private Color _origTrailColor;
        private float _origTrailWidth;
        private float _origTrailDuration;

        private Color _origGlowColor;
        private float _origGlowRange;
        private float _minInactiveLifetime = 0f;

#pragma warning disable CS8618
        public EWCProjectileComponentShooter(IntPtr ptr) : base(ptr) { }
#pragma warning restore CS8618

        [HideFromIl2Cpp]
        public override void Init(ushort playerIndex, ushort ID, Projectile settings, bool isLocal, Vector3 position, Vector3 dir)
        {
            if (enabled) return;

            base.Init(playerIndex, ID, settings, isLocal, position, dir);

            _projectile.transform.SetPositionAndRotation(_position, s_tempRot);

            foreach (var effect in _projectile.m_effectsToStopEmittingOnImpact)
                effect.Play();
            foreach (var go in _projectile.m_toDestroyOnImpact)
                go.active = true;
            _inactiveLifetime = _minInactiveLifetime;

            if (_targeting != null)
            {
                _targeting.m_light.enabled = true;
                _targeting.m_light.Color = Approximately(settings.GlowColor, Color.black) ? _origGlowColor : settings.GlowColor;
                _targeting.m_light.Range = settings.GlowRange < 0f ? _origGlowRange : settings.GlowRange;

                if (_targeting.m_ricochetEffect != null)
                    _targeting.m_ricochetEffect.active = true;
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.enabled = settings.EnableTrail;
                _trailRenderer.startColor = Approximately(settings.TrailColor, Color.black) ? _origTrailColor : settings.TrailColor;
                _trailRenderer.time = settings.TrailTime < 0f ? _origTrailDuration: settings.TrailTime;
                _trailRenderer.widthMultiplier = settings.TrailWidth < 0f ? _origTrailWidth : settings.TrailWidth;
                if (!settings.TrailCullOnDie)
                    _inactiveLifetime = Math.Max(_trailRenderer.time, _inactiveLifetime);
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
            if (_targeting != null)
            {
                if (_targeting.m_light != null)
                {
                    _origGlowColor = _targeting.m_light.Color;
                    _origGlowRange = _targeting.m_light.Range;
                }
                else
                {
                    _targeting.m_light = gameObject.AddComponent<EffectLight>();
                    _targeting.m_light.enabled = false;
                    _origGlowColor = Color.clear;
                    _origGlowRange = 0f;
                }
            }

            // HACK:
            // Projectiles with customized particle effect-lingering stuff don't destroy the gameobject on impact.
            // For those that don't, they always have the gameobject as the only element.
            // That causes trails to linger for the former but not the latter.
            // This code replaces the parent with all the children so the trail always lingers.
            if (_projectile.m_toDestroyOnImpact[0] == gameObject)
            {
                _projectile.m_toDestroyOnImpact.Clear();
                foreach (var child in transform)
                {
                    Transform transform = child.Cast<Transform>();
                    if (transform.name != "Sphere" && transform.name != "Cube")
                        _projectile.m_toDestroyOnImpact.Add(transform.gameObject);
                }
            }
            else
                _minInactiveLifetime = 1.5f; // Some buffer time to let effects clear completely

            if (_trailRenderer != null)
            {
                _origTrailColor = _trailRenderer.startColor;
                _origTrailDuration = _trailRenderer.time;
                _origTrailWidth = _trailRenderer.widthMultiplier;
            }
        }

        public override void SetVisualPosition(Vector3 positionVisual, float lerpDist)
        {
            base.SetVisualPosition(positionVisual, lerpDist);
            _projectile.transform.SetPositionAndRotation(_positionVisual, s_tempRot);
        }

        protected override void Update()
        {
            base.Update();
            if (!enabled) return;

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

            if (_settings!.TrailCullOnDie && _trailRenderer != null)
                _trailRenderer.Clear();

            if (_targeting != null)
            {
                _targeting.m_light.enabled = false;

                if (_targeting.m_ricochetEffect != null)
                    _targeting.m_ricochetEffect.active = false;
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();
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
