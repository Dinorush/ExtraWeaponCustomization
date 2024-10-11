using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public abstract class EWCProjectileComponentBase : MonoBehaviour
    {
        public EWCProjectileComponentBase(IntPtr ptr) : base(ptr)
        {
            Hitbox = new(this);
        }

        public EWCProjectileHitbox Hitbox;

        private float _endLifetime;
        private Coroutine? _inactiveRoutine;
        protected Vector3 _baseDir;
        protected Vector3 _velocity;
        protected float _accel;
        protected float _accelExpo;
        protected float _accelTime;
        protected float _accelProgress;

        protected Vector3 _position;
        protected float _gravity;
        protected float _gravityVel;

        private float _lerpProgress;
        private float _lerpTime;
        private Vector3 _positionVisualDiff;
        protected Vector3 _positionVisual;
        protected Vector3 _dirVisual;

        protected static Quaternion s_tempRot;
        public const float VisualLerpDist = 2f;
        private bool _sendDestroy;
        private ushort _id;

        protected virtual void Awake()
        {
            enabled = false;
        }

        public virtual void Init(ushort ID, Vector3 position, Vector3 velocity, float accel, float accelExpo, float accelTime, float gravity, float scale, float lifetime, bool sendDestroy)
        {
            if (enabled) return;

            if (_inactiveRoutine != null)
                StopCoroutine(_inactiveRoutine);

            gameObject.transform.localScale = Vector3.one * scale;
            s_tempRot.SetLookRotation(velocity);
            gameObject.transform.SetPositionAndRotation(position, s_tempRot);
            gameObject.active = true;
            enabled = true;
            _lerpProgress = 1f;
            _accelProgress = 0f;
            _endLifetime = Time.time + lifetime;
            _position = position;
            _velocity = velocity;
            _baseDir = velocity.normalized;
            _accel = Mathf.Approximately(accel, 1f) ? 1f : accel; // Explicitly check for approx 1 so can skip accel calculations
            _accelExpo = accelExpo;
            _accelTime = accelTime == 0 ? 0.001f : accelTime;
            _gravity = gravity;
            _gravityVel = 0;
            _sendDestroy = sendDestroy;
            _id = ID;
        }

        public void SetVisualPosition(Vector3 positionVisual, float lerpDist = VisualLerpDist)
        {
            if (_velocity.sqrMagnitude == 0) return;

            _lerpProgress = 0f;
            _lerpTime = lerpDist / _velocity.magnitude;
            _positionVisual = positionVisual;
            _positionVisualDiff = _positionVisual - _position;
            _dirVisual = _position + _velocity * _lerpTime - _positionVisual;
            s_tempRot.SetLookRotation(_dirVisual);
            gameObject.transform.SetPositionAndRotation(_positionVisual, s_tempRot);
        }

        protected void LerpVisualOffset()
        {
            if (_lerpProgress == 1f)
            {
                _positionVisual = _position;
                _dirVisual = _velocity.sqrMagnitude > 0.01f ? _velocity : _baseDir * .01f;
                return;
            }

            _lerpProgress = Math.Min(1f, _lerpProgress + Time.deltaTime / _lerpTime);
            float invLerp = 1f - _lerpProgress;
            Vector3 lastPos = _positionVisual;
            _positionVisual = _position + Vector3.Lerp(_positionVisualDiff, Vector3.zero, 1f - invLerp * invLerp);
            _dirVisual = _velocity + _positionVisual - lastPos;
        }

        protected virtual void Update()
        {
            Vector3 deltaVel = _velocity.sqrMagnitude > 0.01f ? _velocity * Time.deltaTime : _baseDir * .01f;
            Hitbox.Update(_position, deltaVel);
            if (!enabled) return; // Died by hitbox

            if (Time.time > _endLifetime)
            {
                Die();
                return;
            }
        }

        public virtual void Die()
        {
            if (!enabled) return;

            gameObject.transform.localScale = Vector3.zero;
            enabled = false;
            _inactiveRoutine = StartCoroutine(DelayedInactive().WrapToIl2Cpp());
            if (_sendDestroy)
                EWCProjectileManager.DoProjectileDestroy(_id);
            Hitbox.Die();
        }

        [HideFromIl2Cpp]
        private IEnumerator DelayedInactive()
        {
            yield return new WaitForSeconds(2f);
            gameObject.active = false;
            _inactiveRoutine = null;
        }
    }
}
