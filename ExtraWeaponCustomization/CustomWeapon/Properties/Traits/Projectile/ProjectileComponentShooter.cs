using System;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public class ProjectileComponentShooter : ProjectileComponentBase
    {
        private ProjectileTargeting _Projectile;

        protected override Vector3 Velocity { get; set; }
        protected override Vector3 Position { get => _Projectile.transform.position; }

#pragma warning disable CS8618
        public ProjectileComponentShooter(IntPtr ptr) : base(ptr) { }
#pragma warning restore CS8618

        private void Awake()
        {
            _Projectile = GetComponent<ProjectileTargeting>();
            _Projectile.OnFire(null);
        }

        protected virtual void FixedUpdate()
        {
            _Projectile.transform.forward = Velocity.normalized;
        }

        protected virtual void Update()
        {
            Vector3 velocity = Velocity;
            velocity.y += _Gravity * Time.deltaTime;
            Velocity = velocity;
        }

        protected void Die()
        {
            
        }
    }
}
