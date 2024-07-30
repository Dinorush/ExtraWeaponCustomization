using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using System;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileComponentShooter : EWCProjectileComponentBase
    {
        private ProjectileBase _projectile;
        private ProjectileTargeting? _targeting;
        private TrailRenderer? _trailRenderer;
        public ProjectileType Type;

#pragma warning disable CS8618
        public EWCProjectileComponentShooter(IntPtr ptr) : base(ptr) { }
#pragma warning restore CS8618

        public override void Init(int characterIndex, ushort ID, Vector3 position, Vector3 velocity, float gravity)
        {
            if (enabled) return;

            base.Init(characterIndex, ID, position, velocity, gravity);

            _trailRenderer?.Clear();
            foreach (var effect in _projectile.m_effectsToStopEmittingOnImpact)
                effect.Play();
            foreach (var go in _projectile.m_toDestroyOnImpact)
                go.active = true;

            if (_targeting != null)
            {
                _targeting.m_light.enabled = true;
                _targeting.m_ricochetEffect.active = true;
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
        }

        protected override void Update()
        {
            _velocity.y -= _Gravity * Time.deltaTime;
            Vector3 velocity = _velocity * Time.deltaTime;
            _position += velocity;
            base.Update();
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
                _targeting.m_light.enabled = false;
                _targeting.m_ricochetEffect.active = false;
            }

            EWCProjectileManager.Shooter.ReturnToPool(this);
        }
    }
}
