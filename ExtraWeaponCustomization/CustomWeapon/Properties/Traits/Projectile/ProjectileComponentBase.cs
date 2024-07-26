using System;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public abstract class ProjectileComponentBase : MonoBehaviour
    {
        public ProjectileComponentBase(IntPtr ptr) : base(ptr) { }

        protected abstract Vector3 Velocity { get; set; }
        protected abstract Vector3 Position { get; }
        protected float _Gravity;

        public void Init(Vector3 velocity, float gravity)
        {
            Velocity = velocity;
            _Gravity = gravity;
        }
    }
}
