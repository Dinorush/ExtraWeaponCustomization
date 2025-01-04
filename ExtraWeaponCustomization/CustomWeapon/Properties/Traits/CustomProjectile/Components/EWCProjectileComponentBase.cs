using EWC.API;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.Utils;
using Il2CppInterop.Runtime.Attributes;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public abstract class EWCProjectileComponentBase : MonoBehaviour
    {
        private static readonly System.Random Random = new();

#pragma warning disable CS8618 // Settings is set on Init call which will always run before it is used
        public EWCProjectileComponentBase(IntPtr ptr) : base(ptr)
        {
            Hitbox = new(this);
            Homing = new(this);
            _inactiveCallback = new(() => _inactiveLifetime, Cleanup);
        }
#pragma warning restore CS8618

        public readonly EWCProjectileHitbox Hitbox;
        public readonly EWCProjectileHoming Homing;
        public Projectile Settings { get; private set; }

        private readonly DelayedCallback _inactiveCallback;
        private float _endLifetime;
        protected float _inactiveLifetime = 0f;
        private Vector3 _dir;
        public Vector3 Dir => _dir;
        private Vector3 _baseVelocity;
        private float _baseSpeed;
        private Vector3 _velocity;
        public Vector3 Velocity => _velocity;
        private float _accelProgress;

        private Vector3 _position;
        public Vector3 Position => _position;
        protected float _gravityVel;

        private float _lerpProgress;
        private float _lerpTime;
        private Vector3 _positionVisualDiff;
        protected Vector3 _positionVisual;
        protected Vector3 _dirVisual;

        protected static Quaternion s_tempRot;
        public bool IsLocal { get; private set; }
        public ushort SyncID { get; private set; }
        public ushort PlayerIndex { get; private set; }


        protected virtual void Awake()
        {
            enabled = false;
        }

        [HideFromIl2Cpp]
        public virtual void Init(ushort playerIndex, ushort ID, Projectile projBase, bool isLocal, Vector3 position, Vector3 dir)
        {
            if (enabled) return;

            _inactiveCallback.Cancel();

            SyncID = ID;
            PlayerIndex = playerIndex;
            Settings = projBase;

            gameObject.transform.localScale = Vector3.one * projBase.ModelScale;
            s_tempRot.SetLookRotation(dir);
            gameObject.transform.SetPositionAndRotation(position, s_tempRot);
            gameObject.active = true;
            enabled = true;

            _lerpProgress = 1f;
            _accelProgress = 0f;
            _endLifetime = Time.time + projBase.Lifetime;

            _position = position;
            _baseSpeed = projBase.MaxSpeed > projBase.MinSpeed ? Random.NextSingle().Lerp(projBase.MinSpeed, projBase.MaxSpeed) : projBase.MinSpeed;
            _velocity = dir * _baseSpeed;
            _baseVelocity = _velocity;
            _dir = dir;
            _gravityVel = 0;
            IsLocal = isLocal;

            Hitbox.Init(projBase);
            Homing.Init(projBase, position, dir);
            ProjectileAPI.FireProjectileSpawnedCallback(this);
        }

        public virtual void SetVisualPosition(Vector3 positionVisual, float lerpDist)
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

        private void LerpVisualOffset()
        {
            if (_lerpProgress == 1f)
            {
                _positionVisual = _position;
                _dirVisual = _velocity.sqrMagnitude > 0.01f ? _velocity : _dir * .01f;
                s_tempRot.SetLookRotation(_dirVisual);
                return;
            }

            _lerpProgress = Math.Min(1f, _lerpProgress + Time.deltaTime / _lerpTime);
            float invLerp = 1f - _lerpProgress;
            Vector3 lastPos = _positionVisual;
            _positionVisual = _position + Vector3.Lerp(_positionVisualDiff, Vector3.zero, 1f - invLerp * invLerp);
            _dirVisual = _velocity + _positionVisual - lastPos;
            s_tempRot.SetLookRotation(_dirVisual);
        }

        protected virtual void Update()
        {
            if (Settings == null) // JFS - if weapon is destroyed, this could possibly happen?
            {
                Die();
                return;
            }

            Homing.Update(_position, ref _dir);
            _baseVelocity = _dir * _baseSpeed;

            Vector3 deltaVel = UpdateVelocity();
            Vector3 collisionVel = deltaVel.sqrMagnitude > EWCProjectileHitbox.MinCollisionSqrDist ? deltaVel : _dir * EWCProjectileHitbox.MinCollisionDist;
            Hitbox.Update(_position, collisionVel);
            if (!enabled) return; // Died by hitbox

            if (Time.time > _endLifetime)
            {
                Die();
                return;
            }

            _position += deltaVel;
            LerpVisualOffset();
        }

        private Vector3 UpdateVelocity()
        {
            float delta = Time.deltaTime;
            float deltaMod;
            if (Settings!.AccelScale != 1f)
            {
                if (_accelProgress == 1f)
                {
                    deltaMod = Settings.AccelScale * delta;
                    _velocity = _baseVelocity * Settings!.AccelScale;
                }
                else // Need to calculate how much distance was covered including the acceleration
                {
                    float trgtProgress = Math.Min(_accelProgress + delta / Settings.AccelTime, 1f);
                    float newExpo = Settings.AccelExponent + 1;
                    float progressMod = (float)(Settings.AccelTime * (Settings.AccelScale - 1.0) * (Math.Pow(trgtProgress, newExpo) - Math.Pow(_accelProgress, newExpo)) / newExpo);
                    if (trgtProgress < 1f)
                        deltaMod = delta + progressMod;
                    else
                    {
                        float timeToAccel = (1f - _accelProgress) * Settings.AccelTime;
                        deltaMod = progressMod + timeToAccel + Settings.AccelScale * (delta - timeToAccel);
                    }
                    _accelProgress = trgtProgress;
                    _velocity = _baseVelocity * Math.Pow(_accelProgress, Settings.AccelExponent).Lerp(1f, Settings.AccelScale);
                }
            }
            else
            {
                _velocity = _baseVelocity;
                deltaMod = delta;
            }

            Vector3 result = _baseVelocity * deltaMod;
            result.y -= 0.5f * Settings.Gravity * delta * delta + _gravityVel * delta;
            _gravityVel += Settings.Gravity * delta;
            _velocity.y -= _gravityVel;

            return result;
        }

        public virtual void Die()
        {
            if (!enabled) return;

            gameObject.transform.localScale = Vector3.zero;
            enabled = false;
            _inactiveCallback.Start();
            EWCProjectileManager.DoProjectileDestroy(PlayerIndex, SyncID, IsLocal);
            Hitbox.Die();
            Homing.Die();
            ProjectileAPI.FireProjectileDestroyedCallback(this);
        }

        protected virtual void Cleanup()
        {
            gameObject.active = false;
        }
    }
}
