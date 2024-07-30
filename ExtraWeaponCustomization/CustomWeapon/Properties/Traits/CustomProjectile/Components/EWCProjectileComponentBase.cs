using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using ExtraWeaponCustomization.Utils;
using System;
using System.Collections;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components
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
        protected Vector3 _dir;
        protected Vector3 _velocity;
        protected Vector3 _position;
        protected float _Gravity;

        private float _lerpProgress;
        private float _lerpTime;
        private Vector3 _positionVisualDiff;
        private Vector3 _dirVisualStart;
        protected Vector3 _positionVisual;
        protected Vector3 _dirVisual;

        protected static Quaternion s_tempRot;
        private const float MaxLifetime = 20f;
        public const float VisualLerpDist = 2f;
        private int _characterIndex;
        private ushort _id;

        protected virtual void Awake()
        {
            enabled = false;
        }

        public virtual void Init(int characterIndex, ushort ID, Vector3 position, Vector3 velocity, float gravity)
        {
            if (enabled) return;

            if (_inactiveRoutine != null)
                CoroutineManager.StopCoroutine(_inactiveRoutine);

            gameObject.transform.localScale = Vector3.one;
            s_tempRot.SetLookRotation(_velocity);
            gameObject.transform.SetPositionAndRotation(position, s_tempRot);
            gameObject.active = true;
            enabled = true;
            _lerpProgress = 1f;
            _endLifetime = Time.time + MaxLifetime;
            _position = position;
            _velocity = velocity;
            _Gravity = gravity;
            _characterIndex = characterIndex;
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
            _dirVisualStart = _dirVisual;
            s_tempRot.SetLookRotation(_dirVisual);
            gameObject.transform.SetPositionAndRotation(_positionVisual, s_tempRot);
        }

        private void LerpVisualOffset()
        {
            if (_lerpProgress == 1f)
            {
                _positionVisual = _position;
                _dirVisual = _velocity;
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
            Hitbox.Update(_position, _velocity * Time.deltaTime);
            if (!enabled) return; // Died by hitbox

            if (Time.time > _endLifetime)
            {
                Die();
                return;
            }

            LerpVisualOffset();
        }

        public virtual void Die()
        {
            if (!enabled) return;

            gameObject.transform.localScale = Vector3.zero;
            enabled = false;
            _inactiveRoutine = CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DelayedInactive()));
            EWCProjectileManager.DoProjectileDestroy(_characterIndex, _id);
            Hitbox.Die();
        }

        private IEnumerator DelayedInactive()
        {
            yield return new WaitForSeconds(2f);
            gameObject.active = false;
            _inactiveRoutine = null;
        }

        private void OnDestroy()
        {
            EWCLogger.Error("Destroy was called for an EWC projectile!");
        }
    }
}
